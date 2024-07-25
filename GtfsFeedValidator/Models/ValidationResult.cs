namespace GtfsFeedValidator.Models
{
    public record ValidationResult
    {
        public ValidationResult(ValidationStatusEnum status)
        {
            Status = status;
        }

        public ValidationStatusEnum Status { get; set; }
        public string? HtmlValidationResult { get; internal set; }

        public Summary? Summary { get; set; }
        public List<Notice>? Notices { get; set; }
    }

    public record Notice
    {
        public string? Code { get; set; }
        public string? Severity { get; set; }
        public long TotalNotices { get; set; }
        public List<SampleNotice>? SampleNotices { get; set; }
    }

    public record SampleNotice
    {
        public long? CsvRowNumber { get; set; }
        public string? ServiceId { get; set; }
        public long? TripCsvRowNumber { get; set; }
        public string? TripId { get; set; }
        public string? RouteId { get; set; }
        public double? SpeedKph { get; set; }
        public double? DistanceKm { get; set; }
        public long? CsvRowNumber1 { get; set; }
        public long? StopSequence1 { get; set; }
        public string? StopId1 { get; set; }
        public string? StopName1 { get; set; }
        public string? DepartureTime1 { get; set; }
        public long? CsvRowNumber2 { get; set; }
        public long? StopSequence2 { get; set; }
        public string? StopId2 { get; set; }
        public string? StopName2 { get; set; }
        public string? ArrivalTime2 { get; set; }
        public string? Filename { get; set; }
        public string? FieldName { get; set; }
        public string? FieldValue { get; set; }
        public string? ShapeId { get; set; }
        public long? StopTimeCsvRowNumber { get; set; }
        public string? StopId { get; set; }
        public string? StopName { get; set; }
        public List<double>? Match { get; set; }
        public double? GeoDistanceToShape { get; set; }
        public string? CurrentDate { get; set; }
        public string? ServiceWindowStartDate { get; set; }
        public string? ServiceWindowEndDate { get; set; }
    }

    public record Summary
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
        public FeedInfo? FeedInfo { get; set; }
        public List<Agency>? Agencies { get; set; }
        public List<string>? Files { get; set; }
        public Counts? Counts { get; set; }
        public List<string>? GtfsFeatures { get; set; }
    }

    public record Agency
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public record Counts
    {
        public long Shapes { get; set; }
        public long Stops { get; set; }
        public long Routes { get; set; }
        public long Trips { get; set; }
        public long Agencies { get; set; }
        public long Blocks { get; set; }
    }

    public record FeedInfo
    {
        public string? PublisherName { get; set; }
        public string? PublisherUrl { get; set; }
        public string? FeedLanguage { get; set; }
    }
}