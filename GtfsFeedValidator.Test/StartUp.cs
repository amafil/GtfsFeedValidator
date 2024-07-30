using GtfsFeedValidator.Configuration;
using GtfsFeedValidator.Endpoints;
using GtfsFeedValidator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GtfsFeedValidator.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddHostedService<GtfsValidatorWorker>();

            builder.Services.Configure<GtfsValidatorSettings>(configuration =>
            {
                configuration.ConnectionString = "Filename=.\\GtfsFeedValidation.db;Connection=shared";
                configuration.GtfsValidatorJarPath = ".\\gtfs-validator-5.0.1-cli.jar";
                configuration.WorkingDirectory = ".\\WorkDir";
            });

            builder.Services.AddScoped<IGtfsFeedValidatorService, GtfsFeedValidatorService>();
            builder.Services.AddSingleton<IGtfsFeedValidatorWorkerService, GtfsFeedValidatorWorkerService>();

            builder.Services.AddHostedService<GtfsValidatorWorker>();

            var app = builder.Build();

            app.MapEndpoints();

            app.Run();
        }
    }
}