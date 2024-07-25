namespace GtfsFeedValidator.Models
{
    public record GtfsFeedValidation
    {
        public string? Id { get; set; }
        public string? FilePath { get; set; }
        public string? Status { get; set; }
        public DateTime InsertDatetime { get; set; }
        public DateTime? StartElaboration { get; set; }
        public DateTime? EndElaboration { get; set; }
        public string? JsonValidationResult { get; internal set; }
        public string? HtmlValidationResult { get; internal set; }
    }
}
