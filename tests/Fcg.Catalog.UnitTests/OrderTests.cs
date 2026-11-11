using Fcg.Catalog.Api.Domain.Common;
using Fcg.Catalog.Api.Domain.Games;
using Fcg.Catalog.Api.Domain.Library;
using Fcg.Catalog.Api.Domain.Orders;

namespace Fcg.Catalog.UnitTests;

public sealed class OrderTests
{
    private readonly FixedTimeProvider _clock = new();
    private readonly Guid _userId = Guid.NewGuid();

    private Game NewGame(decimal price = 59.90m) =>
        Game.Create("Jogo Teste", "desc", "RPG", price, null, _clock);

    [Fact]
    public void Place_CriaPedidoPendingComSnapshotDoPreco()
    {
        var game = NewGame(59.90m);

        var order = Order.Place(_userId, game, _clock);
        game.Update(game.Title, game.Description, game.Genre, 99.90m, null, _clock);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(59.90m, order.Price); // o preço do pedido NÃO acompanha a alteração do catálogo
        Assert.Equal(game.Id, order.GameId);
        Assert.Null(order.ProcessedAt);
    }

    [Fact]
    public void Place_JogoInativo_LancaDomainException()
    {
        var game = NewGame();
        game.Deactivate(_clock);

        Assert.Throws<DomainException>(() => Order.Place(_userId, game, _clock));
    }

    [Fact]
    public void Approve_PedidoPending_MudaParaApproved()
    {
        var order = Order.Place(_userId, NewGame(), _clock);

        order.Approve(_clock);

        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.NotNull(order.ProcessedAt);
    }

    [Fact]
    public void Reject_SemMotivo_UsaMotivoPadrao()
    {
        var order = Order.Place(_userId, NewGame(), _clock);

        order.Reject(null, _clock);

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Pagamento recusado.", order.RejectionReason);
    }

    [Fact]
    public void Transicao_DePedidoJaProcessado_LancaDomainException()
    {
        var order = Order.Place(_userId, NewGame(), _clock);
        order.Approve(_clock);

        Assert.Throws<DomainException>(() => order.Approve(_clock));
        Assert.Throws<DomainException>(() => order.Reject("x", _clock));
    }

    [Fact]
    public void LibraryItem_SoPodeSerConcedidoParaPedidoAprovado()
    {
        var order = Order.Place(_userId, NewGame(), _clock);

        Assert.Throws<DomainException>(() => LibraryItem.Grant(order, _clock));

        order.Approve(_clock);
        var item = LibraryItem.Grant(order, _clock);

        Assert.Equal(_userId, item.UserId);
        Assert.Equal(order.Id, item.OrderId);
    }
}
