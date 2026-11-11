using Fcg.Catalog.Api.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Infrastructure.Persistence;

/// <summary>Popula o catálogo para a demonstração. Desligável por configuração (Seed:SampleGames).</summary>
public sealed class GameCatalogSeeder(
    CatalogDbContext db,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<GameCatalogSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Seed:SampleGames", false) || await db.Games.AnyAsync(cancellationToken))
        {
            return;
        }

        Game[] games =
        [
            Game.Create("Lendas de Aetheria", "RPG de mundo aberto com narrativa ramificada.", "RPG", 149.90m, new DateOnly(2025, 3, 14), clock),
            Game.Create("Neon Drift 2088", "Corrida arcade em uma metrópole cyberpunk.", "Corrida", 89.90m, new DateOnly(2024, 11, 2), clock),
            Game.Create("Cozinha Caótica", "Cooperativo local para até 4 jogadores.", "Party", 39.90m, new DateOnly(2023, 7, 21), clock),
            Game.Create("Ecos do Abismo", "Terror psicológico em uma estação submarina.", "Terror", 59.90m, new DateOnly(2025, 10, 31), clock),
            Game.Create("Pixel Quest", "Plataforma 2D retrô com 120 fases.", "Plataforma", 19.90m, new DateOnly(2022, 5, 5), clock),
            // Acima do limite de aprovação simulado do PaymentsAPI (padrão R$ 300,00): demonstra o fluxo REJEITADO.
            Game.Create("Império Galáctico: Edição de Colecionador", "Estratégia 4X com todas as expansões.", "Estratégia", 499.90m, new DateOnly(2026, 2, 10), clock)
        ];

        db.Games.AddRange(games);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catálogo populado com {Count} jogos de exemplo.", games.Length);
    }
}
