using Fcg.Catalog.Api.Domain.Games;
using Fcg.Catalog.Api.Domain.Library;
using Fcg.Catalog.Api.Domain.Orders;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Catalog.Api.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<LibraryItem> Library => Set<LibraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Outbox (publicar OrderPlacedEvent) + Inbox (consumir PaymentProcessedEvent sem duplicar efeito).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
