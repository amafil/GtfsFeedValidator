namespace GtfsFeedValidator.Models.ValidatorOutput
{
    public record SummaryOutput
    {
        public string? ValidatorVersion { get; set; }
        public string? ValidatedAt { get; set; }
        public string? GtfsInput { get; set; }
        public long Threads { get; set; }
        public string? OutputDirectory { get; set; }
        public string? SystemErrorsReportName { get; set; }
        public string? ValidationReportName { get; set; }
        public string? HtmlReportName { get; set; }
        public string? CountryCode { get; set; }
        public string? DateForValidation { get; set; }
        public FeedInfoOutput? FeedInfo { get; set; }
        public List<AgencyOutput>? Agencies { get; set; }
        public List<string>? Files { get; set; }
        public CountsOutput? Counts { get; set; }
        public List<string>? GtfsFeatures { get; set; }
    }
}
