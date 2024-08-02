using GtfsFeedValidator.Automapper;
using GtfsFeedValidator.Configuration;
using GtfsFeedValidator.Endpoints;
using GtfsFeedValidator.Middleware;
using GtfsFeedValidator.Models;
using GtfsFeedValidator.Services;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GtfsFeedValidator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "GtfsFeedValidator", Version = "v1" });
            options.AddDocumentFilterInstance(new AdditionalSchemasDocumentFilter());
        });
        builder.Services.AddOptions<GtfsValidatorSettings>()
            .BindConfiguration(GtfsValidatorSettings.Path);

        builder.Services.AddScoped<IGtfsFeedValidatorService, GtfsFeedValidatorService>();
        builder.Services.AddSingleton<IGtfsFeedValidatorWorkerService, GtfsFeedValidatorWorkerService>();

        builder.Services.AddHostedService<GtfsValidatorWorker>();

        builder.Services.AddAutoMapper(config =>
        {
            config.AddProfile<AutomapperProfile>();
        }, typeof(Program));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapEndpoints();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.Run();

    }
}

internal class AdditionalSchemasDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        context.SchemaGenerator.GenerateSchema(typeof(GtfsValidatorResponse), context.SchemaRepository);
    }
}