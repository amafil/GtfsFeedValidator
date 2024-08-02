namespace GtfsFeedValidator.Models.ValidatorOutput
{
    public record NoticeOutput
    {
        public string? Code { get; set; }
        public string? Severity { get; set; }
        public long TotalNotices { get; set; }
        public List<SampleNoticeOutput>? SampleNotices { get; set; }
    }
}
