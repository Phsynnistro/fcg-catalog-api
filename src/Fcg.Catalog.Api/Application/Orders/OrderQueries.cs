using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Application.Orders;

public sealed class OrderQueries(CatalogDbContext db)
{
    public async Task<Result<OrderResponse>> GetByIdAsync(Guid orderId, Guid requesterId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        var order = await OrdersWithGame()
            .Where(x => x.Order.Id == orderId)
            .Select(x => new OrderResponse(
                x.Order.Id, x.Order.UserId, x.Order.GameId, x.GameTitle, x.Order.Price,
                x.Order.Status, x.Order.RejectionReason, x.Order.CreatedAt, x.Order.ProcessedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null || (!requesterIsAdmin && order.UserId != requesterId))
        {
            return OrderErrors.NotFound(orderId);
        }

        return order;
    }

    public async Task<PagedResponse<OrderResponse>> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var query = OrdersWithGame().Where(x => x.Order.UserId == userId);
        var total = await query.CountAsync(cancellationToken);

        // Filtrar → ordenar → paginar → projetar: a projeção para record fica por último (o EF traduz tudo para um SQL só).
        var items = await query
            .OrderByDescending(x => x.Order.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OrderResponse(
                x.Order.Id, x.Order.UserId, x.Order.GameId, x.GameTitle, x.Order.Price,
                x.Order.Status, x.Order.RejectionReason, x.Order.CreatedAt, x.Order.ProcessedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderResponse>(items, page, pageSize, total);
    }

    private IQueryable<OrderWithGame> OrdersWithGame() =>
        db.Orders.AsNoTracking().Join(
            db.Games.AsNoTracking(),
            order => order.GameId,
            game => game.Id,
            (order, game) => new OrderWithGame { Order = order, GameTitle = game.Title });

    // Tipo intermediário com member-init: o EF consegue compor Where/OrderBy sobre ele (sobre construtor de record, não).
    private sealed class OrderWithGame
    {
        public required Domain.Orders.Order Order { get; init; }
        public required string GameTitle { get; init; }
    }
}
