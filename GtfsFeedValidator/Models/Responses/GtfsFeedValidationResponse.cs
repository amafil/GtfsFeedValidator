namespace GtfsFeedValidator.Models.Responses
{
    public record GtfsFeedValidationResponse
    {
        public GtfsFeedValidationResponse(ValidationStatusEnum status) => Status = status;

        public ValidationStatusEnum Status { get; set; }

        public GtfsValidatorResponse ValidationResult { get; set; }
    }
}

