using Fcg.Catalog.Api.Domain.Common;
using Fcg.Catalog.Api.Domain.Games;

namespace Fcg.Catalog.UnitTests;

public sealed class GameTests
{
    private readonly FixedTimeProvider _clock = new();

    [Fact]
    public void Create_ArredondaPrecoEFicaAtivo()
    {
        var game = Game.Create("  Jogo  ", null!, "RPG", 10.999m, null, _clock);

        Assert.Equal("Jogo", game.Title);
        Assert.Equal(string.Empty, game.Description);
        Assert.Equal(11.00m, game.Price);
        Assert.True(game.IsActive);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(10000)]
    public void Create_PrecoForaDaFaixa_LancaDomainException(double price) =>
        Assert.Throws<DomainException>(() => Game.Create("Jogo", "", "RPG", (decimal)price, null, _clock));

    [Fact]
    public void Deactivate_EhIdempotente()
    {
        var game = Game.Create("Jogo", "", "RPG", 10m, null, _clock);

        game.Deactivate(_clock);
        game.Deactivate(_clock);

        Assert.False(game.IsActive);
    }
}
