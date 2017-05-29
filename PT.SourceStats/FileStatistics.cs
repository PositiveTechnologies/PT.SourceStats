using System.Collections.Generic;

namespace PT.SourceStats
{
    public class FileStatistics
    {
        public string FileName { get; set; }

        public Dictionary<string, int> ClassUsings { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> MethodInvocations { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> Includes { get; set; } = new Dictionary<string, int>();

        public FileStatistics()
        {
        }
    }
}
