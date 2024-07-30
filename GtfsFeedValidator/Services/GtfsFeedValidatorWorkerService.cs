using GtfsFeedValidator.Configuration;
using GtfsFeedValidator.Database;
using LiteDB;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace GtfsFeedValidator.Services
{
    public class GtfsFeedValidatorWorkerService(IOptions<GtfsValidatorSettings> configuration, ILogger<IGtfsFeedValidatorService> logger) : IGtfsFeedValidatorWorkerService
    {
        public async Task ProcessQueueAsync(CancellationToken stoppingToken)
        {
            string gtfsValidatorJarFullPath = Path.GetFullPath(configuration.Value.GtfsValidatorJarPath);
            using var db = new LiteDatabase(configuration.Value.ConnectionString);
            {
                var gtfsFeedValidationCollection = db.GetCollection<GtfsFeedValidation>(Constants.GtfsFeedValidationCollectionName);
                GtfsFeedValidation feed = gtfsFeedValidationCollection.Query()
                    .Where(x => x.Status == Constants.GtfsFeedValidationStatusPending)
                    .OrderBy(o => o.InsertDatetime)
                    .Limit(1)
                    .FirstOrDefault();

                if (feed != null)
                {
                    logger.LogInformation("Processing feed {feedId}", feed.Id);
                    feed.Status = Constants.GtfsFeedValidationStatusProcessing;
                    feed.StartElaboration = DateTime.Now;
                    gtfsFeedValidationCollection.Update(feed);

                    try
                    {
                        string directoryPath = Path.GetDirectoryName(feed.FilePath);
                        string outputPath = Path.Combine(directoryPath, "validationResult");

                        //java -jar .\gtfs-validator-5.0.1-cli.jar -i "gtfs_file.zip" -o "validazione"
                        logger.LogInformation($"-jar \"{gtfsValidatorJarFullPath}\" -i \"{feed.FilePath}\" -o \"{outputPath}\"");
                        using var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "java",
                                Arguments = $"-jar \"{gtfsValidatorJarFullPath}\" -i \"{feed.FilePath}\" -o \"{outputPath}\"",
                                RedirectStandardOutput = false,
                                RedirectStandardError = false,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        process.WaitForExit();
                        process.Close();

                        string sessionId = Guid.NewGuid().ToString().Replace("-", "");

                        var validationResultCollection = db.GetCollection<GtfsFeedValidationResult>(Constants.GtfsFeedValidationResultCollectionName);

                        string reportJson = await File.ReadAllTextAsync(Path.Combine(outputPath, "report.json"), stoppingToken);
                        string reportHtml = await File.ReadAllTextAsync(Path.Combine(outputPath, "report.html"), stoppingToken);

                        validationResultCollection.Insert(new GtfsFeedValidationResult
                        {
                            Id = sessionId,
                            InsertDatetime = DateTime.Now,
                            GtfsFeedValidationId = feed.Id,
                            JsonValidationResult = reportJson,
                            HtmlValidationResult = reportHtml
                        });

                        validationResultCollection.EnsureIndex(x => x.Id);
                        validationResultCollection.EnsureIndex(x => x.GtfsFeedValidationId);

                        feed.Status = Constants.GtfsFeedValidationStatusCompleted;
                    }
                    catch (Exception ex)
                    {
                        feed.Status = Constants.GtfsFeedValidationStatusError;
                        logger.LogError(ex, $"Error executing validation for feedId: {feed.Id}");
                    }
                    finally
                    {
                        feed.EndElaboration = DateTime.Now;
                        gtfsFeedValidationCollection.Update(feed);
                    }

                    // pulizia cartelle ed elaborazioni più vecchie di 24 ore
                    try
                    {
                        var feeds = gtfsFeedValidationCollection.FindAll();
                        foreach (var obsoleteFeed in feeds)
                        {
                            if (obsoleteFeed.EndElaboration.HasValue && obsoleteFeed.EndElaboration.Value.AddHours(24) < DateTime.Now)
                            {
                                string directoryPath = Path.GetDirectoryName(feed.FilePath);

                                Directory.Delete(directoryPath, recursive: true);

                                gtfsFeedValidationCollection.Delete(obsoleteFeed.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during cleanup of obsolete processing");

                    }
                }
            }
        }
    }

    public interface IGtfsFeedValidatorWorkerService
    {
        Task ProcessQueueAsync(CancellationToken stoppingToken);
    }
}
