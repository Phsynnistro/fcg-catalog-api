using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Domain.Orders;

namespace Fcg.Catalog.Api.Application.Orders;

public sealed record PlaceOrderRequest(Guid GameId);

public sealed record OrderResponse(
    Guid Id,
    Guid UserId,
    Guid GameId,
    string GameTitle,
    decimal Price,
    OrderStatus Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ProcessedAt);

public static class OrderErrors
{
    public static Error GameNotFound(Guid gameId) =>
        Error.NotFound("orders.game_not_found", $"Jogo {gameId} não encontrado ou indisponível.");

    public static readonly Error GameAlreadyOwned =
        Error.Conflict("orders.game_already_owned", "Este jogo já está na sua biblioteca.");

    public static readonly Error PurchaseInProgress =
        Error.Conflict("orders.purchase_in_progress", "Já existe uma compra deste jogo aguardando pagamento.");

    // 404 (e não 403) para pedido de outro usuário: não confirma que o recurso existe (evita IDOR por enumeração).
    public static Error NotFound(Guid orderId) =>
        Error.NotFound("orders.not_found", $"Pedido {orderId} não encontrado.");
}
