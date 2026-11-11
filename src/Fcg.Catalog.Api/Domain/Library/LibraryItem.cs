using Fcg.Catalog.Api.Domain.Common;
using Fcg.Catalog.Api.Domain.Orders;

namespace Fcg.Catalog.Api.Domain.Library;

/// <summary>Um jogo na biblioteca de um usuário. Só existe se houver um pedido aprovado que o justifique.</summary>
public sealed class LibraryItem
{
    private LibraryItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime AcquiredAt { get; private set; }

    public static LibraryItem Grant(Order order, TimeProvider clock)
    {
        if (order.Status != OrderStatus.Approved)
        {
            throw new DomainException("Somente pedidos aprovados concedem acesso ao jogo.");
        }

        return new LibraryItem
        {
            Id = Guid.CreateVersion7(),
            UserId = order.UserId,
            GameId = order.GameId,
            OrderId = order.Id,
            AcquiredAt = clock.GetUtcNow().UtcDateTime
        };
    }
}
