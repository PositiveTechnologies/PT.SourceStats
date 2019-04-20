using PT.PM.Common;
using PT.PM.PhpParseTreeUst;
using System.Collections.Generic;

namespace PT.SourceStats
{
    public class PhpStatistics : LanguageStatistics
    {
        public override Language Language => Language.Php;

        public Dictionary<string, string> FilesContent { get; set; } = new Dictionary<string, string>();

        public List<string> HtaccessStrings { get; set; } = new List<string>();

        public Dictionary<string, int> ClassUsings { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> MethodInvocations { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> Includes { get; set; } = new Dictionary<string, int>();

        public PhpStatistics()
        {
        }
    }
}
