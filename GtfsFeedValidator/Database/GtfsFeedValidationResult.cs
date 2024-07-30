using GtfsFeedValidator.Models;

namespace GtfsFeedValidator.Database
{
    public record GtfsFeedValidationResult()
    {
        public required string Id { get; set; }
        public required string GtfsFeedValidationId { get; set; }
        public required DateTime InsertDatetime { get; set; }
        public required string JsonValidationResult { get; set; }
        public required string HtmlValidationResult { get; set; }
    }
}
