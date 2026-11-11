using System.Security.Claims;
using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Application.Games;
using Fcg.Catalog.Api.Infrastructure.Security;

namespace Fcg.Catalog.Api.Presentation.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games").WithTags("Games");

        group.MapGet("", ListAsync)
            .AllowAnonymous()
            .WithName("ListGames")
            .WithSummary("Lista o catálogo (paginado, com busca por título e filtro por gênero)")
            .Produces<PagedResponse<GameResponse>>();

        group.MapGet("/{id:guid}", GetByIdAsync)
            .AllowAnonymous()
            .WithName("GetGameById")
            .Produces<GameResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", CreateAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateGame")
            .WithSummary("Cadastra um jogo (Admin)")
            .Produces<GameResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("UpdateGame")
            .WithSummary("Atualiza um jogo (Admin)")
            .Produces<GameResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeactivateAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteGame")
            .WithSummary("Remove o jogo do catálogo — soft delete (Admin)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal user, GameService service, CancellationToken cancellationToken,
        string? search = null, string? genre = null, bool includeInactive = false, int page = 1, int pageSize = 20)
    {
        // includeInactive só é respeitado para Admin: usuário comum nunca vê jogo removido.
        var result = await service.ListAsync(search, genre, includeInactive && user.IsAdmin(), page, pageSize, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, GameService service, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    private static async Task<IResult> CreateAsync(GameRequest request, GameService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Created($"/api/games/{result.Value.Id}", result.Value) : result.Error!.ToProblem();
    }

    private static async Task<IResult> UpdateAsync(Guid id, GameRequest request, GameService service, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    private static async Task<IResult> DeactivateAsync(Guid id, GameService service, CancellationToken cancellationToken)
    {
        var result = await service.DeactivateAsync(id, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }
}
