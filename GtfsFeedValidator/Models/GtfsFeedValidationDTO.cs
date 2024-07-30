namespace GtfsFeedValidator.Models
{
    public record GtfsFeedValidationDTO(ValidationStatusEnum status)
    {
        public ValidationStatusEnum Status { get; set; } = status;
        public string HtmlValidationResult { get; set; }
        public string JsonValidationResult { get; set; }
    }
}
