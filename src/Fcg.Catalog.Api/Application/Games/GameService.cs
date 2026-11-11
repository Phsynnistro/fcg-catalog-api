using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Domain.Games;
using Fcg.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Application.Games;

/// <summary>CRUD de jogos. CRUD simples não precisa de um handler por operação — seria cerimônia.</summary>
public sealed class GameService(CatalogDbContext db, TimeProvider clock, ILogger<GameService> logger)
{
    public async Task<PagedResponse<GameResponse>> ListAsync(
        string? search, string? genre, bool includeInactive, int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var query = db.Games.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(g => g.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(g => g.Title.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var normalizedGenre = genre.Trim().ToLower();
            query = query.Where(g => g.Genre.ToLower() == normalizedGenre);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(g => g.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GameResponse(g.Id, g.Title, g.Description, g.Genre, g.Price, g.ReleaseDate, g.IsActive, g.CreatedAt, g.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<GameResponse>(items, page, pageSize, total);
    }

    public async Task<Result<GameResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (game is null)
        {
            return GameErrors.NotFound(id);
        }

        return game.ToResponse();
    }

    public async Task<Result<GameResponse>> CreateAsync(GameRequest request, CancellationToken cancellationToken)
    {
        if (request.Validate() is { } validationError)
        {
            return validationError;
        }

        var game = Game.Create(request.Title, request.Description ?? string.Empty, request.Genre, request.Price, request.ReleaseDate, clock);
        db.Games.Add(game);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return GameErrors.TitleAlreadyExists;
        }

        logger.LogInformation("Jogo {GameId} criado: {Title}.", game.Id, game.Title);
        return game.ToResponse();
    }

    public async Task<Result<GameResponse>> UpdateAsync(Guid id, GameRequest request, CancellationToken cancellationToken)
    {
        if (request.Validate() is { } validationError)
        {
            return validationError;
        }

        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (game is null)
        {
            return GameErrors.NotFound(id);
        }

        game.Update(request.Title, request.Description ?? string.Empty, request.Genre, request.Price, request.ReleaseDate, clock);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return GameErrors.TitleAlreadyExists;
        }

        return game.ToResponse();
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (game is null)
        {
            return GameErrors.NotFound(id);
        }

        game.Deactivate(clock);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Jogo {GameId} removido do catálogo (soft delete).", id);
        return Result.Success();
    }
}
