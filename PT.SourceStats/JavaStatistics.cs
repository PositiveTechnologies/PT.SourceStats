using PT.PM.Common;
using PT.PM.JavaParseTreeUst;
using System.Collections.Generic;

namespace PT.SourceStats
{
    public class JavaStatistics : LanguageStatistics
    {
        public override Language Language => Language.Java;

        public int FilesCount { get; set; } = 0;

        public int SourceFilesCount { get; set; } = 0;

        public int JavaFilesCount { get; set; } = 0;

        public int ClassFilesCount { get; set; } = 0;

        public int JspFilesCount { get; set; } = 0;

        public long JavaSourceSize { get; set; } = 0;

        public long SourceCodeLinesCount { get; set; } = 0;

        public long ClassSourceSize { get; set; } = 0;

        public long JspSourceSize { get; set; } = 0;

        public long XHtmlFileCount { get; set; } = 0;

        public HashSet<string> DependencyManagers { get; set; } = new HashSet<string>();

        public HashSet<string> Repositories { get; set; } = new HashSet<string>();

        public HashSet<string> BuildTools { get; set; } = new HashSet<string>();

        public HashSet<string> BuildToolsPlugins { get; set; } = new HashSet<string>();

        public HashSet<string> Dependencies { get; set; } = new HashSet<string>();

        public JavaStatistics()
        {
        }
    }
}
