namespace GtfsFeedValidator.Database
{
    public record GtfsFeedValidation
    {
        public required string Id { get; set; }
        public required string FilePath { get; set; }
        public required string Status { get; set; }
        public required DateTime InsertDatetime { get; set; }
        public DateTime? StartElaboration { get; set; }
        public DateTime? EndElaboration { get; set; }
    }
}
