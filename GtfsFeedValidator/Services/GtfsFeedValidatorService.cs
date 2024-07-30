using GtfsFeedValidator.Configuration;
using GtfsFeedValidator.Database;
using GtfsFeedValidator.Models;
using LiteDB;
using Microsoft.Extensions.Options;

namespace GtfsFeedValidator.Services
{
    public class GtfsFeedValidatorService(IOptions<GtfsValidatorSettings> configuration, ILogger<IGtfsFeedValidatorService> logger) : IGtfsFeedValidatorService
    {
        public async Task<string> StartValidationAsync(IFormFile file)
        {
            string workingDirectoryFullPath = PrepareWorkingDirectory(configuration.Value.WorkingDirectory);

            string sessionId = Guid.NewGuid().ToString().Replace("-", "");

            string tmpFolderPath = Path.Combine(workingDirectoryFullPath, sessionId);
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
            using var db = new LiteDatabase(configuration.Value.ConnectionString);
            var col = db.GetCollection<GtfsFeedValidation>(Constants.GtfsFeedValidationCollectionName);

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

        public GtfsFeedJsonValidationResponse GetJsonValidationResult(string gtfsFeedValidationId)
        {
            GtfsFeedValidationDTO gtfsFeedValidation = GetValidationResult(gtfsFeedValidationId);

            var result = new GtfsFeedJsonValidationResponse(gtfsFeedValidation.Status);

            if (!string.IsNullOrWhiteSpace(gtfsFeedValidation.JsonValidationResult))
            {
                result.ValidationResult = System.Text.Json.JsonSerializer.Deserialize<GtfsValidatorOutput>(
                gtfsFeedValidation.JsonValidationResult,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result.ValidationResult is null)
                {
                    return new GtfsFeedJsonValidationResponse(ValidationStatusEnum.Error);
                }
            }

            return result;
        }

        public GtfsFeedHtmlValidationResponse GetHtmlValidationResult(string gtfsFeedValidationId)
        {
            GtfsFeedValidationDTO gtfsFeedValidation = GetValidationResult(gtfsFeedValidationId);

            return new GtfsFeedHtmlValidationResponse(gtfsFeedValidation.Status)
            {
                ValidationResult = gtfsFeedValidation.HtmlValidationResult
            };
        }

        private GtfsFeedValidationDTO GetValidationResult(string gtfsFeedValidationId)
        {
            using var db = new LiteDatabase(configuration.Value.ConnectionString);
            var gtfsFeedValidationCollection = db.GetCollection<GtfsFeedValidation>(Constants.GtfsFeedValidationCollectionName);

            var gtfsFeedValidation = gtfsFeedValidationCollection.FindById(gtfsFeedValidationId);

            if (gtfsFeedValidation == null)
            {
                return new GtfsFeedValidationDTO(ValidationStatusEnum.NotFund);
            }

            if (gtfsFeedValidation.Status == Constants.GtfsFeedValidationStatusError)
            {
                return new GtfsFeedValidationDTO(ValidationStatusEnum.Error);
            }

            var gtfsFeedJsonResultValidationCollection = db.GetCollection<GtfsFeedValidationResult>(Constants.GtfsFeedValidationResultCollectionName);
            GtfsFeedValidationResult gtfsFeedJsonResultValidation = gtfsFeedJsonResultValidationCollection
                .Query()
                .Where(f => f.GtfsFeedValidationId == gtfsFeedValidation.Id)
                .FirstOrDefault();

            if (gtfsFeedJsonResultValidation is null)
            {
                return new GtfsFeedValidationDTO(ValidationStatusEnum.Awaiting);
            }

            return new GtfsFeedValidationDTO(ValidationStatusEnum.Completed)
            {
                JsonValidationResult = gtfsFeedJsonResultValidation.JsonValidationResult,
                HtmlValidationResult = gtfsFeedJsonResultValidation.HtmlValidationResult
            }; ;
        }

        private static string PrepareWorkingDirectory(string workingDirectory)
        {
            var result = Path.GetFullPath(workingDirectory);

            if (!Path.IsPathFullyQualified(result))
            {
                throw new ArgumentException("Working directory path must be fully qualified", nameof(workingDirectory));
            }

            if (!Directory.Exists(workingDirectory) && !string.IsNullOrWhiteSpace(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            return result;
        }
    }

    public interface IGtfsFeedValidatorService
    {
        Task<string> StartValidationAsync(IFormFile file);
        GtfsFeedJsonValidationResponse GetJsonValidationResult(string gtfsFeedValidationId);
        GtfsFeedHtmlValidationResponse GetHtmlValidationResult(string gtfsFeedValidationId);

    }
}
