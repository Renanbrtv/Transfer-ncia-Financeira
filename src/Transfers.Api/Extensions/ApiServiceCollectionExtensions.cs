using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Transfers.Api.Middleware;
using Transfers.Api.Workers;
using Transfers.Application.Options;

namespace Transfers.Api.Extensions;

internal static class ApiServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TransferLimitsOptions>(configuration.GetSection(TransferLimitsOptions.SectionName));
        services.Configure<ScheduledTransfersOptions>(configuration.GetSection(ScheduledTransfersOptions.SectionName));

        services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier));
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Location", "Idempotency-Replayed");
        }));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Transfers API",
                Version = "v1",
                Description = "Transferências imediatas e agendadas entre contas, com cheque especial, "
                    + "limites por hora (dia/noite) e controle de concorrência."
            });

            IncludeXmlComments(options, typeof(Program).Assembly);
            IncludeXmlComments(options, typeof(TransferLimitsOptions).Assembly);
        });

        services.AddHostedService<ScheduledTransfersWorker>();

        return services;
    }

    private static void IncludeXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, Assembly assembly)
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(path))
        {
            options.IncludeXmlComments(path);
        }
    }
}
