namespace GtfsFeedValidator.Models.ValidatorOutput
{
    public record AgencyOutput
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
