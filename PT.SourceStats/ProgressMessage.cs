namespace PT.SourceStats
{
    public class ProgressMessage : Message
    {
        public override MessageType MessageType => MessageType.Progress;

        public int ProcessedCount { get; set; }

        public int TotalCount { get; set; }

        public string LastFileName { get; set; }

        public ProgressMessage()
        {
        }

        public ProgressMessage(int processedCount, int totalCount)
        {
            ProcessedCount = processedCount;
            TotalCount = totalCount;
        }
    }
}
