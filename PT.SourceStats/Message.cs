using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PT.SourceStats
{
    public abstract class Message
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public abstract MessageType MessageType { get; }
    }
}
