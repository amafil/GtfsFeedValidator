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
            string javaExecutablePath = string.IsNullOrWhiteSpace(configuration.Value.JavaExecutablePath)
                ? "java"
                : configuration.Value.JavaExecutablePath.Trim();
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
                        string directoryPath = Path.GetDirectoryName(feed.FilePath)
                            ?? throw new InvalidOperationException($"Feed file path '{feed.FilePath}' has no parent directory.");
                        string outputPath = Path.Combine(directoryPath, "validationResult");
                        string jsonReportPath = Path.Combine(outputPath, "report.json");
                        string htmlReportPath = Path.Combine(outputPath, "report.html");

                        if (!File.Exists(feed.FilePath))
                        {
                            throw new FileNotFoundException("The uploaded GTFS feed no longer exists.", feed.FilePath);
                        }

                        if (!File.Exists(gtfsValidatorJarFullPath))
                        {
                            throw new FileNotFoundException("The configured GTFS validator JAR was not found.", gtfsValidatorJarFullPath);
                        }

                        if (Path.IsPathFullyQualified(javaExecutablePath) && !File.Exists(javaExecutablePath))
                        {
                            throw new FileNotFoundException("The configured Java executable was not found.", javaExecutablePath);
                        }

                        ProcessExecutionResult javaVersionResult = await RunJavaProcessAsync(javaExecutablePath, "-version");
                        string javaVersionOutput = FormatProcessOutput(javaVersionResult);
                        logger.LogInformation(
                            "Java executable {javaExecutablePath}, version for feed {feedId} (exit code {exitCode}):{newline}{output}",
                            javaExecutablePath,
                            feed.Id,
                            javaVersionResult.ExitCode,
                            Environment.NewLine,
                            javaVersionOutput);

                        if (javaVersionResult.ExitCode != 0)
                        {
                            throw new InvalidOperationException(
                                $"Could not read the Java version. Exit code: {javaVersionResult.ExitCode}. " +
                                FormatProcessOutput(javaVersionResult));
                        }

                        logger.LogInformation(
                            "Starting GTFS validator for feed {feedId}, input {inputPath}, output {outputPath}",
                            feed.Id,
                            feed.FilePath,
                            outputPath);

                        ProcessExecutionResult validatorResult = await RunJavaProcessAsync(
                            javaExecutablePath,
                            "-jar",
                            gtfsValidatorJarFullPath,
                            "-i",
                            feed.FilePath,
                            "-o",
                            outputPath);
                        string validatorOutput = FormatProcessOutput(validatorResult);
                        logger.LogInformation(
                            "GTFS validator output for feed {feedId} (exit code {exitCode}):{newline}{output}",
                            feed.Id,
                            validatorResult.ExitCode,
                            Environment.NewLine,
                            validatorOutput);

                        if (validatorResult.ExitCode != 0)
                        {
                            throw new InvalidOperationException(
                                $"GTFS validator exited with code {validatorResult.ExitCode}. {validatorOutput}");
                        }

                        string[] missingReports = new[] { jsonReportPath, htmlReportPath }
                            .Where(path => !File.Exists(path))
                            .ToArray();
                        if (missingReports.Length > 0)
                        {
                            throw new InvalidOperationException(
                                $"GTFS validator exited successfully but did not create the expected report(s): " +
                                $"{string.Join(", ", missingReports)}. " +
                                validatorOutput);
                        }

                        string sessionId = Guid.NewGuid().ToString().Replace("-", "");

                        var validationResultCollection = db.GetCollection<GtfsFeedValidationResult>(Constants.GtfsFeedValidationResultCollectionName);

                        string reportJson = await File.ReadAllTextAsync(jsonReportPath, stoppingToken);
                        string reportHtml = await File.ReadAllTextAsync(htmlReportPath, stoppingToken);

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
                        logger.LogError(ex, "Error executing validation for feedId: {feedId}", feed.Id);
                    }
                    finally
                    {
                        feed.EndElaboration = DateTime.Now;
                        gtfsFeedValidationCollection.Update(feed);
                    }

                    // pulizia cartelle ed elaborazioni più vecchie di 24 ore
                    try
                    {
                        // Materialize the results before deleting from the same collection.
                        var feeds = gtfsFeedValidationCollection.FindAll().ToList();
                        DateTime cleanupThreshold = DateTime.Now.AddHours(-24);

                        foreach (var obsoleteFeed in feeds)
                        {
                            if (obsoleteFeed.EndElaboration.HasValue && obsoleteFeed.EndElaboration.Value < cleanupThreshold)
                            {
                                string? directoryPath = Path.GetDirectoryName(obsoleteFeed.FilePath);

                                if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
                                {
                                    Directory.Delete(directoryPath, recursive: true);
                                }

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

        private static string TruncateProcessOutput(string output)
        {
            const int maxLength = 4000;
            return output.Length <= maxLength
                ? output
                : output[..maxLength] + "... (truncated)";
        }

        private static async Task<ProcessExecutionResult> RunJavaProcessAsync(
            string javaExecutablePath,
            params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = javaExecutablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("The Java process could not be started.");
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new ProcessExecutionResult(
                process.ExitCode,
                await standardOutputTask,
                await standardErrorTask);
        }

        private static string FormatProcessOutput(ProcessExecutionResult result)
        {
            string standardOutput = TruncateProcessOutput(result.StandardOutput).Trim();
            string standardError = TruncateProcessOutput(result.StandardError).Trim();

            return $"stdout: {(string.IsNullOrEmpty(standardOutput) ? "<empty>" : standardOutput)}" +
                $"{Environment.NewLine}stderr: {(string.IsNullOrEmpty(standardError) ? "<empty>" : standardError)}";
        }

        private sealed record ProcessExecutionResult(
            int ExitCode,
            string StandardOutput,
            string StandardError);
    }

    public interface IGtfsFeedValidatorWorkerService
    {
        Task ProcessQueueAsync(CancellationToken stoppingToken);
    }
}
