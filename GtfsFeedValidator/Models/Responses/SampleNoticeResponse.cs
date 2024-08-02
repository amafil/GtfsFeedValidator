namespace GtfsFeedValidator.Models.Responses
{
    public record SampleNoticeResponse
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
}
