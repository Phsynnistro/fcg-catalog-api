using Fcg.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Application.Library;

public sealed record LibraryItemResponse(Guid GameId, string Title, string Genre, Guid OrderId, DateTime AcquiredAt);

public sealed class LibraryQueries(CatalogDbContext db)
{
    public async Task<IReadOnlyList<LibraryItemResponse>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Library.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Join(db.Games.AsNoTracking(), item => item.GameId, game => game.Id, (item, game) => new { item, game })
            .OrderByDescending(x => x.item.AcquiredAt)
            .Select(x => new LibraryItemResponse(x.game.Id, x.game.Title, x.game.Genre, x.item.OrderId, x.item.AcquiredAt))
            .ToListAsync(cancellationToken);
}
