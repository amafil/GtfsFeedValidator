namespace GtfsFeedValidator.Models.ValidatorOutput
{
    public record FeedInfoOutput
    {
        public string? PublisherName { get; set; }
        public string? PublisherUrl { get; set; }
        public string? FeedLanguage { get; set; }
    }
}
