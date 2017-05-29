using PT.PM.Common;

namespace PT.SourceStats
{
    public interface IFileStatisticsCollector
    {
        ILogger Logger { get; set; }

        FileStatistics CollectInfo(string fileName);
    }
}
