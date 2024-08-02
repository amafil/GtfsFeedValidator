namespace GtfsFeedValidator.Models.Responses
{
    public record AgencyResponse
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
