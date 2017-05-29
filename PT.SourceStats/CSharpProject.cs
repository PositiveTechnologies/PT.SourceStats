using System.Collections.Generic;

namespace PT.SourceStats
{
    public class CSharpProject
    {
        public string Name { get; set; }

        public string GUID { get; set; }

        public string RelativePath { get; set; }

        public string ProjectType { get; set; }

        public string FrameworkVersion { get; set; }

        public List<string> References { get; set; } = new List<string>();

        public List<string> Dependencies { get; set; } = new List<string>();

        public int FilesCount { get; set; } = 0;

        public int SourceFilesCount { get; set; } = 0;

        public int CsFilesCount { get; set; } = 0;

        public int AspxFilesCount { get; set; } = 0;

        public int CsHtmlFilesCount { get; set; } = 0;

        public int AshxFilesCount { get; set; } = 0;

        public int AscxFilesCount { get; set; } = 0;

        public int LinesCount { get; set; } = 0;

        public int CsLinesCount { get; set; } = 0;

        public int AspxLinesCount { get; set; } = 0;

        public int CsHtmlLinesCount { get; set; } = 0;

        public int AshxLinesCount { get; set; } = 0;

        public int AscxLinesCount { get; set; } = 0;

        public CSharpProject()
        {
        }
    }
}
