using GtfsFeedValidator.Configuration;
using GtfsFeedValidator.Endpoints;
using GtfsFeedValidator.Middleware;
using GtfsFeedValidator.Services;

namespace GtfsFeedValidator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddOptions<GtfsValidatorSettings>()
            .BindConfiguration(GtfsValidatorSettings.Path);

        builder.Services.AddScoped<IGtfsFeedValidatorService, GtfsFeedValidatorService>();
        builder.Services.AddSingleton<IGtfsFeedValidatorWorkerService, GtfsFeedValidatorWorkerService>();

        builder.Services.AddHostedService<GtfsValidatorWorker>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
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