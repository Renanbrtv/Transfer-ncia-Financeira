using Transfers.Api.Extensions;
using Transfers.Application;
using Transfers.Domain.Limits;
using Transfers.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApi(builder.Configuration)
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

// Falha rápida: uma configuração de limites inválida (fuso inexistente, valores <= 0, dia após a noite)
// derruba a aplicação na subida, e não na primeira transferência.
_ = app.Services.GetRequiredService<TransferLimitPolicy>();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAsync(app.Lifetime.ApplicationStopping);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Swagger:Enabled", defaultValue: true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Transfers API v1");
        options.DocumentTitle = "Transfers API";
    });
}

app.UseCors(ApiServiceCollectionExtensions.FrontendCorsPolicy);

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .ExcludeFromDescription();

app.Run();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program
{
}
