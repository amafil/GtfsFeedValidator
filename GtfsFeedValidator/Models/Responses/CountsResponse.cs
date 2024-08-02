namespace GtfsFeedValidator.Models.Responses
{
    public record CountsResponse
    {
        public long Shapes { get; set; }
        public long Stops { get; set; }
        public long Routes { get; set; }
        public long Trips { get; set; }
        public long Agencies { get; set; }
        public long Blocks { get; set; }
    }
}
