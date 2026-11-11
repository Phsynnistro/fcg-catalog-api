using Fcg.Catalog.Api.Application.Orders;
using Fcg.Contracts.Events;
using MassTransit;

namespace Fcg.Catalog.Api.Presentation.Consumers;

/// <summary>
/// Consumer = ponto de entrada, igual a um endpoint HTTP: só traduz a mensagem e delega ao caso de uso.
/// O handler recebe o MESMO DbContext (scoped) que o outbox usa — por isso a atualização do pedido,
/// o registro no inbox e eventuais mensagens de saída são commitados juntos.
/// </summary>
public sealed class PaymentProcessedConsumer(ProcessPaymentResultHandler handler) : IConsumer<PaymentProcessedEvent>
{
    public Task Consume(ConsumeContext<PaymentProcessedEvent> context) =>
        handler.HandleAsync(context.Message, context.CancellationToken);
}
