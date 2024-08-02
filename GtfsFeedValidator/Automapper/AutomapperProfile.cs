using AutoMapper;
using GtfsFeedValidator.Models;
using GtfsFeedValidator.Models.Responses;
using GtfsFeedValidator.Models.ValidatorOutput;

namespace GtfsFeedValidator.Automapper
{
    public class AutomapperProfile : Profile
    {
        public AutomapperProfile()
        {
            CreateMap<FeedInfoOutput, FeedInfoResponse>();

            CreateMap<AgencyOutput, AgencyResponse>();

            CreateMap<CountsOutput, CountsResponse>();

            CreateMap<SummaryOutput, SummaryResponse>();

            CreateMap<SampleNoticeOutput, SampleNoticeResponse>();

            CreateMap<NoticeOutput, NoticeResponse>();

            CreateMap<GtfsValidatorOutput, GtfsValidatorResponse>();
        }
    }
}
