namespace GtfsFeedValidator.Models.Responses
{
    public record GtfsFeedHtmlValidationResponse
    {
        public GtfsFeedHtmlValidationResponse(ValidationStatusEnum status) => Status = status;

        public ValidationStatusEnum Status { get; set; }

        public string ValidationResult { get; set; }
    }
}
