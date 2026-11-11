using System.Security.Claims;
using Fcg.Catalog.Api.Application.Library;
using Fcg.Catalog.Api.Infrastructure.Security;

namespace Fcg.Catalog.Api.Presentation.Endpoints;

public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/library", async (ClaimsPrincipal user, LibraryQueries queries, CancellationToken cancellationToken) =>
                Results.Ok(await queries.ListForUserAsync(user.GetUserId(), cancellationToken)))
            .RequireAuthorization()
            .WithTags("Library")
            .WithName("GetMyLibrary")
            .WithSummary("Jogos na biblioteca do usuário autenticado")
            .Produces<IReadOnlyList<LibraryItemResponse>>();

        return app;
    }
}
