using GtfsFeedValidator.Models;
using GtfsFeedValidator.Models.Responses;
using GtfsFeedValidator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
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
            .WithName("Request GTFS Validation")
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Description = "Request a GTFS validation";

                generatedOperation.Responses.Remove(StatusCodes.Status200OK.ToString());
                generatedOperation.Responses.Add("500", new OpenApiResponse { Description = "Error during feed validation enqueuing" });
                generatedOperation.Responses.Add("202",
                    new OpenApiResponse
                    {
                        Description = "Feed validation enqueued",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "application/json",
                                new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "uuid"
                                    }
                                }
                            }
                        }
                    });

                return generatedOperation;
            });
            
            app.MapGet("/validation-result/{gtfsFeedValidationId}",
                ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService, string gtfsFeedValidationId) =>
                {
                    GtfsFeedValidationResponse validation = gtfsFeedValidatorService.GetJsonValidationResult(gtfsFeedValidationId);

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

                    return Results.Ok(validation.ValidationResult);
                })
            .WithName("Get GTFS Validation Result")
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Description = "Get the validation result";

                generatedOperation.Responses.Add("204", new OpenApiResponse { Description = "Validation is still in progress" });
                generatedOperation.Responses.Add("404", new OpenApiResponse { Description = "Validation not found" });
                generatedOperation.Responses.Add("500", new OpenApiResponse { Description = "Error during feed validation" });

                var parameter = generatedOperation.Parameters[0];

                parameter.Description = "Elaboration id returned from /start-validation";
                parameter.Required = true;

                return generatedOperation;
            });

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
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Description = "Download the validation result file in HTML format";

                generatedOperation.Responses.Add("204", new OpenApiResponse { Description = "Validation is still in progress" });
                generatedOperation.Responses.Add("404", new OpenApiResponse { Description = "Validation not found" });
                generatedOperation.Responses.Add("500", new OpenApiResponse { Description = "Error during feed validation" });

                var parameter = generatedOperation.Parameters[0];

                parameter.Description = "Elaboration id returned from /start-validation";
                parameter.Required = true;

                return generatedOperation;
            });

            // apiStatus con le informazioni della versione del feed ecc e tutte le elaborazioni
            app.MapGet("/api-status",
                ([FromServices] IGtfsFeedValidatorService gtfsFeedValidatorService) =>
                {
                    return Results.Ok(gtfsFeedValidatorService.GetApiStatus());
                });
        }
    }
}
