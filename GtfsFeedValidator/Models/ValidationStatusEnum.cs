using System.Text.Json.Serialization;

namespace GtfsFeedValidator.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ValidationStatusEnum
    {
        NotFund,
        Error,
        Awaiting,
        Completed
    }
}
