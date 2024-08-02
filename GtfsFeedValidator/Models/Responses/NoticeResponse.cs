using GtfsFeedValidator.Models.ValidatorOutput;

namespace GtfsFeedValidator.Models.Responses
{
    public record NoticeResponse
    {
        public string? Code { get; set; }
        public string? Severity { get; set; }
        public long TotalNotices { get; set; }
        public List<SampleNoticeResponse>? SampleNotices { get; set; }
    }
}
