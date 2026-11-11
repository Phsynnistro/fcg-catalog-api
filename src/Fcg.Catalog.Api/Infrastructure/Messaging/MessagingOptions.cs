namespace Fcg.Catalog.Api.Infrastructure.Messaging;

/// <summary>
/// Nome de fila vem de configuração (ConfigMap). Convenção: &lt;serviço-dono&gt;.&lt;evento&gt;.
/// Cada serviço consumidor tem a SUA fila ligada ao exchange do evento: é assim que o mesmo
/// PaymentProcessedEvent chega ao Catalog E ao Notifications (pub/sub com fan-out).
/// </summary>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public CatalogQueues Queues { get; init; } = new();
    public int PrefetchCount { get; init; } = 16;
    public int ConcurrentMessageLimit { get; init; } = 8;
}

public sealed class CatalogQueues
{
    public string PaymentProcessed { get; init; } = "catalog.payment-processed";
}
