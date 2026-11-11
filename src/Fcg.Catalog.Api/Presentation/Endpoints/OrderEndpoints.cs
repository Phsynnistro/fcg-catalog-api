using System.Security.Claims;
using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Application.Orders;
using Fcg.Catalog.Api.Infrastructure.Security;

namespace Fcg.Catalog.Api.Presentation.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("", PlaceAsync)
            .WithName("PlaceOrder")
            .WithSummary("Inicia a compra de um jogo (adicionar à biblioteca). Assíncrono: retorna 202 + Location.")
            .Produces<OrderResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetOrderById")
            .WithSummary("Acompanha o status de um pedido (Pending → Approved | Rejected)")
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("", ListMineAsync)
            .WithName("ListMyOrders")
            .WithSummary("Pedidos do usuário autenticado")
            .Produces<PagedResponse<OrderResponse>>();

        return app;
    }

    private static async Task<IResult> PlaceAsync(
        PlaceOrderRequest request, ClaimsPrincipal user, PlaceOrderHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(user.GetUserId(), user.GetEmail(), request, cancellationToken);
        return result.IsSuccess
            ? Results.Accepted($"/api/orders/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> GetByIdAsync(Guid id, ClaimsPrincipal user, OrderQueries queries, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, user.GetUserId(), user.IsAdmin(), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    private static async Task<IResult> ListMineAsync(
        ClaimsPrincipal user, OrderQueries queries, CancellationToken cancellationToken, int page = 1, int pageSize = 20) =>
        Results.Ok(await queries.ListForUserAsync(user.GetUserId(), page, pageSize, cancellationToken));
}
