using Fcg.Catalog.Api.Application.Common;
using Fcg.Catalog.Api.Domain.Games;

namespace Fcg.Catalog.Api.Application.Games;

public sealed record GameRequest(string Title, string? Description, string Genre, decimal Price, DateOnly? ReleaseDate);

public sealed record GameResponse(
    Guid Id,
    string Title,
    string Description,
    string Genre,
    decimal Price,
    DateOnly? ReleaseDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class GameErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("games.not_found", $"Jogo {id} não encontrado.");

    public static readonly Error TitleAlreadyExists =
        Error.Conflict("games.title_in_use", "Já existe um jogo com este título.");
}

public static class GameMappings
{
    public static GameResponse ToResponse(this Game game) => new(
        game.Id, game.Title, game.Description, game.Genre, game.Price,
        game.ReleaseDate, game.IsActive, game.CreatedAt, game.UpdatedAt);

    public static Error? Validate(this GameRequest request) => new ValidationBuilder()
        .AddIf(string.IsNullOrWhiteSpace(request.Title), "title", "O título é obrigatório.")
        .AddIf(request.Title?.Trim().Length > Game.TitleMaxLength, "title", $"O título deve ter até {Game.TitleMaxLength} caracteres.")
        .AddIf(request.Description?.Trim().Length > Game.DescriptionMaxLength, "description", $"A descrição deve ter até {Game.DescriptionMaxLength} caracteres.")
        .AddIf(string.IsNullOrWhiteSpace(request.Genre), "genre", "O gênero é obrigatório.")
        .AddIf(request.Genre?.Trim().Length > Game.GenreMaxLength, "genre", $"O gênero deve ter até {Game.GenreMaxLength} caracteres.")
        .AddIf(request.Price is < 0 or > Game.MaxPrice, "price", $"O preço deve estar entre 0 e {Game.MaxPrice}.")
        .Build();
}
