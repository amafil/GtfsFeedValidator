using GtfsFeedValidator.Configuration;
using GtfsFeedValidator.Models;
using LiteDB;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace GtfsFeedValidator.Services
{
    public class GtfsFeedValidatorService : IGtfsFeedValidatorService
    {
        private readonly GtfsValidatorSettings _configuration;
        private readonly ILogger<IGtfsFeedValidatorService> _logger;

        public GtfsFeedValidatorService(IOptions<GtfsValidatorSettings> configuration, ILogger<IGtfsFeedValidatorService> logger)
        {
            _configuration = configuration.Value;
            _logger = logger;
        }

        public async Task<string> StartValidationAsync(IFormFile file)
        {
            PrepareWorkingDirectory(_configuration.WorkingDirectory);

            string sessionId = Guid.NewGuid().ToString().Replace("-", "");

            string tmpFolderPath = Path.Combine(_configuration.WorkingDirectory, sessionId);
            if (!Directory.Exists(tmpFolderPath))
            {
                Directory.CreateDirectory(tmpFolderPath);
            }

            string tempFilePath = Path.Combine(
                tmpFolderPath,
                file.FileName);

            using var stream = File.OpenWrite(tempFilePath);
            await file.CopyToAsync(stream);

            // Open database (or create if doesn't exist)
            using var db = new LiteDatabase(_configuration.ConnectionString);
            var col = db.GetCollection<GtfsFeedValidation>(Constants.ValidationQueueCollectionName);

            var validation = new GtfsFeedValidation
            {
                Id = sessionId,
                FilePath = tempFilePath,
                Status = Constants.GtfsFeedValidationStatusPending,
                InsertDatetime = DateTime.Now,
                StartElaboration = null
            };

            col.Insert(validation);
            col.EnsureIndex(x => x.Id);
            col.EnsureIndex(x => x.Status);

            return sessionId;
        }

        public ValidationResult GetValidationResult(string sessionId)
        {
            using var db = new LiteDatabase(_configuration.ConnectionString);
            var col = db.GetCollection<GtfsFeedValidation>(Constants.ValidationQueueCollectionName);

            var validation = col.FindById(sessionId);

            if (validation == null)
            {
                return new ValidationResult(ValidationStatusEnum.NotFund);
            }

            if (validation.Status == Constants.GtfsFeedValidationStatusError)
            {
                return new ValidationResult(ValidationStatusEnum.Error);
            }

            if (validation.JsonValidationResult is null || string.IsNullOrWhiteSpace(validation.JsonValidationResult))
            {
                return new ValidationResult(ValidationStatusEnum.Awaiting);
            }

            var validationResult = System.Text.Json.JsonSerializer.Deserialize<ValidationResult>(
                validation.JsonValidationResult,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if(validationResult is null)
            {
                return new ValidationResult(ValidationStatusEnum.Error);
            }

            validationResult.HtmlValidationResult = validation.HtmlValidationResult;
            validationResult.Status = ValidationStatusEnum.Completed;

            return validationResult;
        }

        public async Task ProcessQueueAsync(CancellationToken stoppingToken)
        {
            using var db = new LiteDatabase(_configuration.ConnectionString);
            {
                var collection = db.GetCollection<GtfsFeedValidation>(Constants.ValidationQueueCollectionName);
                var feed = collection.Query()
                    .Where(x => x.Status == Constants.GtfsFeedValidationStatusPending)
                    .OrderBy(o => o.InsertDatetime)
                    .Limit(1)
                    .FirstOrDefault();

                if (feed != null)
                {
                    _logger.LogInformation("Processing feed {feedId}", feed.Id);
                    feed.Status = Constants.GtfsFeedValidationStatusProcessing;
                    feed.StartElaboration = DateTime.Now;
                    collection.Update(feed);

                    try
                    {
                        string directoryPath = Path.GetDirectoryName(feed.FilePath);
                        string outputPath = Path.Combine(directoryPath, "validationResult");

                        //java -jar .\gtfs-validator-5.0.1-cli.jar -i "gtfs_file.zip" -o "validazione"
                        using var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "java.exe",
                                Arguments = $"-jar \"{_configuration.GtfsValidatorJarPath}\" -i \"{feed.FilePath}\" -o \"{outputPath}\"",
                                RedirectStandardOutput = false,
                                RedirectStandardError = false,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        process.WaitForExit();
                        process.Close();

                        string reportJson = await File.ReadAllTextAsync(Path.Combine(outputPath, "report.json"), stoppingToken);
                        feed.JsonValidationResult = reportJson;

                        string reportHtml = await File.ReadAllTextAsync(Path.Combine(outputPath, "report.html"), stoppingToken);
                        feed.HtmlValidationResult = reportHtml;

                        feed.Status = Constants.GtfsFeedValidationStatusCompleted;
                    }
                    catch (Exception ex)
                    {
                        feed.Status = Constants.GtfsFeedValidationStatusError;
                        _logger.LogError(ex, $"Errore in esecuzione validatore GTFS per il feed {feed.Id}");
                    }
                    finally
                    {
                        feed.EndElaboration = DateTime.Now;
                        collection.Update(feed);
                    }

                    // pulizia cartelle ed elaborazioni più vecchie di 24 ore
                    try
                    {
                        var feeds = collection.FindAll();
                        foreach (var obsoleteFeed in feeds)
                        {
                            if (obsoleteFeed.EndElaboration.HasValue && obsoleteFeed.EndElaboration.Value.AddHours(24) < DateTime.Now)
                            {
                                string directoryPath = Path.GetDirectoryName(feed.FilePath);

                                Directory.Delete(directoryPath, recursive: true);

                                collection.Delete(obsoleteFeed.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore in fase di pulizia delle elaborazioni obsolete");
                    }
                }
            }
        }

        private static void PrepareWorkingDirectory(string workingDirectory)
        {
            if (!Path.IsPathFullyQualified(workingDirectory))
            {
                throw new ArgumentException("Working directory path must be fully qualified", nameof(workingDirectory));
            }

            if (!Directory.Exists(workingDirectory) && !string.IsNullOrWhiteSpace(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }
        }
    }

    public interface IGtfsFeedValidatorService
    {
        Task<string> StartValidationAsync(IFormFile file);
        ValidationResult GetValidationResult(string sessionId);
        Task ProcessQueueAsync(CancellationToken stoppingToken);
    }
}
