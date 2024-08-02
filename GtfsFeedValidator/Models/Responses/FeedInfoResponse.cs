namespace GtfsFeedValidator.Models.Responses
{
    public record FeedInfoResponse
    {
        public string? PublisherName { get; set; }
        public string? PublisherUrl { get; set; }
        public string? FeedLanguage { get; set; }
    }
}
