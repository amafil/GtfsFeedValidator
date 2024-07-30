using GtfsFeedValidator.Database;
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
             * Method for uploading the GTFS file, returns a unique session ID
             * 
             * 1. Collects the GTFS file and saves it in a temporary folder
             * 2. Writes the file path and its status to LiteDB
             * 3. Returns the gtfsFeedValidationId
             * 
             */
            app.MapPost("/start-validation",
                async ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, IFormFile file) =>
                {
                    string gtfsFeedValidationId = await gtfsFeedValidatorService.StartValidationAsync(file);

                    return Results.Accepted(uri: "/validation-result", gtfsFeedValidationId);
                })
            .DisableAntiforgery()
            .WithName("Start GTFS Validation")
            .WithOpenApi();

            /*
             * Method to retrieve the status of a processing and the validation result
             * 
             * Takes the gtfsFeedValidationId ID as input
             */
            app.MapGet("/validation-result/{gtfsFeedValidationId}",
                ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, string gtfsFeedValidationId) =>
                {
                    GtfsFeedJsonValidationResponse validation = gtfsFeedValidatorService.GetJsonValidationResult(gtfsFeedValidationId);

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

                    return Results.Ok(validation);
                })
            .WithName("Get GTFS Validation Result")
            .WithOpenApi();

            /*
             * Method for downloading the validation in HTML format
             * 
             * Takes the session ID as input
             */
            app.MapGet("/validation-result/download/{gtfsFeedValidationId}",
                ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, string gtfsFeedValidationId) =>
                {
                    GtfsFeedHtmlValidationResponse validation = gtfsFeedValidatorService.GetHtmlValidationResult(gtfsFeedValidationId);

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

                    byte[] reportHtml = Encoding.UTF8.GetBytes(validation.ValidationResult);

                    return Results.File(reportHtml, contentType: "text/html", fileDownloadName: "report.html");
                })
            .WithName("Download HTML Validation Status")
            .WithOpenApi();

        }
    }
}
