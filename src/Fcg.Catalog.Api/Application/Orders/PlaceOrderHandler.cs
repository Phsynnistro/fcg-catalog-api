using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Domain.Orders;
using Fcg.Contracts.Events;
using Fcg.Catalog.Api.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Application.Orders;

/// <summary>
/// Caso de uso: iniciar a compra ("adicionar jogo à biblioteca").
///
/// Decisões que um avaliador deve enxergar aqui:
///  1. UserId vem do TOKEN, nunca do corpo da requisição → ninguém compra "em nome de" outro usuário.
///  2. Price vem do BANCO do catálogo, nunca do cliente → ninguém compra um jogo por R$ 0,01.
///  3. Order (Pending) + OrderPlacedEvent são gravados na MESMA transação (Transactional Outbox).
///  4. A resposta é 202 Accepted: o resultado é assíncrono (consistência eventual). O cliente acompanha em GET /api/orders/{id}.
/// </summary>
public sealed class PlaceOrderHandler(
    CatalogDbContext db,
    IPublishEndpoint publishEndpoint,
    TimeProvider clock,
    ILogger<PlaceOrderHandler> logger)
{
    public async Task<Result<OrderResponse>> HandleAsync(
        Guid userId, string userEmail, PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.GameId == Guid.Empty)
        {
            return Error.Validation(new Dictionary<string, string[]> { ["gameId"] = ["Informe o jogo a ser comprado."] });
        }

        var game = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GameId && g.IsActive, cancellationToken);
        if (game is null)
        {
            return OrderErrors.GameNotFound(request.GameId);
        }

        if (await db.Library.AnyAsync(l => l.UserId == userId && l.GameId == game.Id, cancellationToken))
        {
            return OrderErrors.GameAlreadyOwned;
        }

        if (await db.Orders.AnyAsync(o => o.UserId == userId && o.GameId == game.Id && o.Status == OrderStatus.Pending, cancellationToken))
        {
            return OrderErrors.PurchaseInProgress;
        }

        var order = Order.Place(userId, game, clock);
        db.Orders.Add(order);

        await publishEndpoint.Publish(
            new OrderPlacedEvent(order.Id, order.UserId, order.GameId, order.Price, userEmail, game.Title, order.CreatedAt),
            cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Corrida entre dois cliques simultâneos: o índice único parcial (UserId, GameId) WHERE Status='Pending' barra o segundo.
            return OrderErrors.PurchaseInProgress;
        }

        logger.LogInformation("Pedido {OrderId} criado (Pending) para o jogo {GameId}; OrderPlacedEvent gravado no outbox.", order.Id, game.Id);

        return new OrderResponse(order.Id, order.UserId, order.GameId, game.Title, order.Price,
            order.Status, order.RejectionReason, order.CreatedAt, order.ProcessedAt);
    }
}
