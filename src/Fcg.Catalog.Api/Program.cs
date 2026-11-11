using Fcg.Catalog.Api.Application;
using Fcg.Catalog.Api.Infrastructure;
using Fcg.Catalog.Api.Infrastructure.Persistence;
using Fcg.Catalog.Api.Presentation;
using Fcg.Catalog.Api.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiDefaults()
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapApiDocs("FCG • Catalog API");
app.MapHealthEndpoints();
app.MapGameEndpoints();
app.MapOrderEndpoints();
app.MapLibraryEndpoints();

await app.Services.InitializeDatabaseAsync<CatalogDbContext>(app.Lifetime.ApplicationStopping);
await app.RunAsync();
