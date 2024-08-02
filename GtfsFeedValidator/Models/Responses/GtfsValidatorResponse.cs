using GtfsFeedValidator.Models.Responses;

namespace GtfsFeedValidator.Models
{
    public record GtfsValidatorResponse
    {
        public SummaryResponse? Summary { get; set; }
        public List<NoticeResponse>? Notices { get; set; }
    }
}