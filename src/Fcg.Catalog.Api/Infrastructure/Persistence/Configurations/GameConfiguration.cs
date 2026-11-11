using Fcg.Catalog.Api.Domain.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.Title).HasMaxLength(Game.TitleMaxLength).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(Game.DescriptionMaxLength).IsRequired();
        builder.Property(g => g.Genre).HasMaxLength(Game.GenreMaxLength).IsRequired();
        builder.Property(g => g.Price).HasPrecision(10, 2); // dinheiro = decimal com precisão explícita, nunca double
        builder.Property(g => g.IsActive).IsRequired();

        builder.HasIndex(g => g.Title).IsUnique();
        builder.HasIndex(g => g.Genre);
    }
}
