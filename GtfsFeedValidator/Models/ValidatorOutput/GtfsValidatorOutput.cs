namespace GtfsFeedValidator.Models.ValidatorOutput
{
    public record GtfsValidatorOutput
    {
        public SummaryOutput? Summary { get; set; }
        public List<NoticeOutput>? Notices { get; set; }
    }
}