namespace GtfsFeedValidator.Models.Responses
{
    public record SummaryResponse
    {
        public string? ValidatedAt { get; set; }
        public string? CountryCode { get; set; }
        public FeedInfoResponse? FeedInfo { get; set; }
        public List<AgencyResponse>? Agencies { get; set; }
        public List<string>? Files { get; set; }
        public CountsResponse? Counts { get; set; }
        public List<string>? GtfsFeatures { get; set; }
    }
}
