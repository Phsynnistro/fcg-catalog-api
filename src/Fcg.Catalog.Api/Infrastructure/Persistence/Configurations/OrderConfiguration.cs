using Fcg.Catalog.Api.Domain.Games;
using Fcg.Catalog.Api.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Price).HasPrecision(10, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.RejectionReason).HasMaxLength(Order.RejectionReasonMaxLength);

        builder.HasOne<Game>().WithMany().HasForeignKey(o => o.GameId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.UserId, o.CreatedAt });

        // Índice único PARCIAL (recurso do PostgreSQL): no máximo UM pedido Pending por (usuário, jogo).
        // É isso que garante, sob concorrência real, que dois cliques não gerem duas cobranças.
        builder.HasIndex(o => new { o.UserId, o.GameId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'")
            .HasDatabaseName("UX_Orders_UserId_GameId_Pending");
    }
}
