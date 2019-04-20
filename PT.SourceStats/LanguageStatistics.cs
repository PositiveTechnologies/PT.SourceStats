using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PT.PM.Common;

namespace PT.SourceStats
{
    public abstract class LanguageStatistics
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public abstract Language Language { get; }
    }
}
