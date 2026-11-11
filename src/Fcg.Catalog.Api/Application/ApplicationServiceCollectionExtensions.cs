using Fcg.Catalog.Api.Application.Games;
using Fcg.Catalog.Api.Application.Library;
using Fcg.Catalog.Api.Application.Orders;

namespace Fcg.Catalog.Api.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<GameService>();
        services.AddScoped<PlaceOrderHandler>();
        services.AddScoped<ProcessPaymentResultHandler>();
        services.AddScoped<OrderQueries>();
        services.AddScoped<LibraryQueries>();

        return services;
    }
}
