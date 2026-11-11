using Fcg.Catalog.Api.Domain.Common;
using Fcg.Catalog.Api.Domain.Games;

namespace Fcg.Catalog.Api.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Pedido de compra. É a "memória" local da transação distribuída: nasce Pending e só sai desse
/// estado quando o PaymentProcessedEvent chega. Máquina de estados: Pending → Approved | Rejected.
/// </summary>
public sealed class Order
{
    public const int RejectionReasonMaxLength = 300;

    private Order()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }

    /// <summary>Snapshot do preço no momento da compra. Se o jogo mudar de preço depois, o pedido não muda.</summary>
    public decimal Price { get; private set; }

    public OrderStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public static Order Place(Guid userId, Game game, TimeProvider clock)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário inválido.");
        }

        if (!game.IsActive)
        {
            throw new DomainException("Jogo indisponível para compra.");
        }

        return new Order
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            GameId = game.Id,
            Price = game.Price,
            Status = OrderStatus.Pending,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
    }

    public void Approve(TimeProvider clock)
    {
        EnsurePending();
        Status = OrderStatus.Approved;
        ProcessedAt = clock.GetUtcNow().UtcDateTime;
    }

    public void Reject(string? reason, TimeProvider clock)
    {
        EnsurePending();
        var text = string.IsNullOrWhiteSpace(reason) ? "Pagamento recusado." : reason.Trim();
        Status = OrderStatus.Rejected;
        RejectionReason = text.Length > RejectionReasonMaxLength ? text[..RejectionReasonMaxLength] : text;
        ProcessedAt = clock.GetUtcNow().UtcDateTime;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"O pedido {Id} já foi processado (status atual: {Status}).");
        }
    }
}
