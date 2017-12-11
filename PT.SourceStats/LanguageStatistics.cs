using Newtonsoft.Json;
using PT.PM.Common;
using PT.PM.Common.Json;

namespace PT.SourceStats
{
    public abstract class LanguageStatistics
    {
        [JsonConverter(typeof(LanguageJsonConverter))]
        public abstract Language Language { get; }
    }
}
