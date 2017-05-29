using System.Collections.Generic;

namespace PT.SourceStats
{
    public class StatisticsMessage : Message
    {
        public override MessageType MessageType => MessageType.Result;

        public string Id { get; set; }

        public int ErrorCount { get; set; }

        public IList<LanguageStatistics> LanguageStatistics { get; set; }

        public StatisticsMessage()
        {
        }
    }
}
