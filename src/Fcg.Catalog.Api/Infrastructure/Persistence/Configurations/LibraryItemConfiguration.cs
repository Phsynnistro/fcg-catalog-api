using Fcg.Catalog.Api.Domain.Games;
using Fcg.Catalog.Api.Domain.Library;
using Fcg.Catalog.Api.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class LibraryItemConfiguration : IEntityTypeConfiguration<LibraryItem>
{
    public void Configure(EntityTypeBuilder<LibraryItem> builder)
    {
        builder.ToTable("LibraryItems");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.HasOne<Game>().WithMany().HasForeignKey(l => l.GameId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Restrict);

        // Um usuário possui cada jogo no máximo uma vez — garantido pelo banco, não só pelo código.
        builder.HasIndex(l => new { l.UserId, l.GameId }).IsUnique();
    }
}
