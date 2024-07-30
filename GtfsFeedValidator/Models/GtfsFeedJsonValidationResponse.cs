namespace GtfsFeedValidator.Models
{
    public record GtfsFeedJsonValidationResponse
    {
        public GtfsFeedJsonValidationResponse(ValidationStatusEnum status) => Status = status;

        public ValidationStatusEnum Status { get; set; }
        public GtfsValidatorOutput ValidationResult { get; set; }
    }
}

