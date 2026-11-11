using Fcg.Catalog.Api.Domain.Common;

namespace Fcg.Catalog.Api.Domain.Games;

public sealed class Game
{
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 2000;
    public const int GenreMaxLength = 60;
    public const decimal MaxPrice = 9999.99m;

    private Game()
    {
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Genre { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public DateOnly? ReleaseDate { get; private set; }

    /// <summary>
    /// Soft delete: um jogo vendido não pode sumir — pedidos e bibliotecas o referenciam.
    /// "Excluir" = tirar do catálogo (IsActive = false).
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Game Create(string title, string description, string genre, decimal price, DateOnly? releaseDate, TimeProvider clock)
    {
        var game = new Game
        {
            Id = Guid.CreateVersion7(),
            IsActive = true,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        game.Apply(title, description, genre, price, releaseDate);
        return game;
    }

    public void Update(string title, string description, string genre, decimal price, DateOnly? releaseDate, TimeProvider clock)
    {
        Apply(title, description, genre, price, releaseDate);
        UpdatedAt = clock.GetUtcNow().UtcDateTime;
    }

    public void Deactivate(TimeProvider clock)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = clock.GetUtcNow().UtcDateTime;
    }

    private void Apply(string title, string description, string genre, decimal price, DateOnly? releaseDate)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > TitleMaxLength)
        {
            throw new DomainException($"O título é obrigatório e deve ter até {TitleMaxLength} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(genre) || genre.Trim().Length > GenreMaxLength)
        {
            throw new DomainException($"O gênero é obrigatório e deve ter até {GenreMaxLength} caracteres.");
        }

        if (price is < 0 or > MaxPrice)
        {
            throw new DomainException($"O preço deve estar entre 0 e {MaxPrice}.");
        }

        Title = title.Trim();
        Description = (description ?? string.Empty).Trim();
        Genre = genre.Trim();
        Price = decimal.Round(price, 2);
        ReleaseDate = releaseDate;
    }
}
