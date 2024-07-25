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
            // FIXME: missing configuration injection
            //builder.Services.AddOptions<GtfsValidatorSettings>()
            //    .Bind(configuration);

            builder.Services.AddScoped<IGtfsFeedValidatorService, GtfsFeedValidatorService>();

            var app = builder.Build();

            app.MapEndpoints();

            app.Run();
        }
    }
}