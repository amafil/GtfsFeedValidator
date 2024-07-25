using GtfsFeedValidator.Models;
using GtfsFeedValidator.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace GtfsFeedValidator.Endpoints
{
    public static class Endpoints
    {
        public static void MapEndpoints(this IEndpointRouteBuilder app)
        {
            /*
             * Metodo per l'upload del gtfs, ritorna un id univoco di sessione
             * 
             * 1. raccoglie il file gtfs e lo salva in una cartella temporanea
             * 2. su LiteDB scrive il path del file da elaborare e il suo stato
             * 3. ritorna l'id della sessione
             * 
             */
            app.MapPost("/start-validation",
                async ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, IFormFile file) =>
                {
                    string sessionId = await gtfsFeedValidatorService.StartValidationAsync(file);

                    return Results.Accepted(uri: "/validation-status", sessionId);
                })
            .DisableAntiforgery()
            .WithName("Start GTFS Validation")
            .WithOpenApi();

            /*
             * Metodo per recuperare lo stato di un'elaborazione e il risultato della validazione
             * 
             * In input prende l'id della sessione
             */
            app.MapGet("/validation-result/{sessionId}",
                ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, string sessionId) =>
                {
                    ValidationResult validation = gtfsFeedValidatorService.GetValidationResult(sessionId);

                    return HandleValidationResult(validation, file: false);
                })
            .WithName("Get GTFS Validation Result")
            .WithOpenApi();

            /*
             * Metodo per scaricare la validazione in formato html
             * 
             * In input prende l'id della sessione
             */
            app.MapGet("/validation-status/download/{sessionId}",
                ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, string sessionId) =>
                {

                    ValidationResult validation = gtfsFeedValidatorService.GetValidationResult(sessionId);

                    return HandleValidationResult(validation, file: true);
                })
            .WithName("Download HTML Validation Status")
            .WithOpenApi();

        }

        private static IResult HandleValidationResult(ValidationResult validation, bool file)
        {
            if (validation.Status == ValidationStatusEnum.NotFund)
            {
                return Results.NotFound();
            }

            if (validation.Status == ValidationStatusEnum.Error)
            {
                return Results.Problem(
                    title: "Feed Validation Error",
                    detail: "There was an error during feed validation",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            if (validation.Status == ValidationStatusEnum.Awaiting)
            {
                return Results.NoContent();
            }

            if (file)
            {
                byte[] reportHtml = Encoding.UTF8.GetBytes(validation.HtmlValidationResult);

                return Results.File(reportHtml, contentType: "text/html", fileDownloadName: "report.html");
            }

            return Results.Ok(validation);
        }
    }
}
