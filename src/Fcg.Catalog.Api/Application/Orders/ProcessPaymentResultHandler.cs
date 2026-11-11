using Fcg.Catalog.Api.Domain.Library;
using Fcg.Catalog.Api.Domain.Orders;
using Fcg.Contracts.Events;
using Fcg.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Application.Orders;

/// <summary>
/// Conclui a compra ao receber o PaymentProcessedEvent.
///
/// Idempotência em camadas (o RabbitMQ entrega "at-least-once" — duplicatas VÃO acontecer):
///  1. Inbox do MassTransit: descarta MessageId já processado;
///  2. Máquina de estados: pedido fora de Pending é ignorado;
///  3. Índice único (UserId, GameId) na biblioteca.
/// </summary>
public sealed class ProcessPaymentResultHandler(
    CatalogDbContext db,
    TimeProvider clock,
    ILogger<ProcessPaymentResultHandler> logger)
{
    public async Task HandleAsync(PaymentProcessedEvent message, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order is null)
        {
            // Não deveria acontecer (o pedido é gravado antes do evento existir). Lançar faz a mensagem
            // passar pelo retry e, persistindo, ir para a fila _error — onde alguém investiga. Engolir esconderia o bug.
            throw new InvalidOperationException($"Pedido {message.OrderId} não encontrado para o resultado de pagamento.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.LogInformation("Pedido {OrderId} já estava {Status}; mensagem duplicada ignorada.", order.Id, order.Status);
            return;
        }

        if (message.Status == PaymentStatus.Approved)
        {
            order.Approve(clock);

            var alreadyOwned = await db.Library.AnyAsync(l => l.UserId == order.UserId && l.GameId == order.GameId, cancellationToken);
            if (!alreadyOwned)
            {
                db.Library.Add(LibraryItem.Grant(order, clock));
            }

            logger.LogInformation("Pagamento aprovado: jogo {GameId} adicionado à biblioteca do usuário {UserId} (pedido {OrderId}).",
                order.GameId, order.UserId, order.Id);
        }
        else
        {
            order.Reject(message.Reason, clock);
            logger.LogInformation("Pagamento recusado para o pedido {OrderId}: {Reason}.", order.Id, order.RejectionReason);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
