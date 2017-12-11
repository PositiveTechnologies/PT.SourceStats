using PT.PM.Common;
using PT.PM.CSharpParseTreeUst;
using System.Collections.Generic;

namespace PT.SourceStats
{
    public class CSharpStatistics : LanguageStatistics
    {
        public override Language Language => CSharp.Language;

        public int FilesCount { get; set; } = 0;

        public int CsFilesCount { get; set; } = 0;

        public int AspxFilesCount { get; set; } = 0;

        public int CsHtmlFilesCount { get; set; } = 0;

        public int AshxFilesCount { get; set; } = 0;

        public int AscxFilesCount { get; set; } = 0;

        public int SourceFilesCount { get; set; } = 0;

        public int LinesCount { get; set; } = 0;

        public int CsLinesCount { get; set; } = 0;

        public int AspxLinesCount { get; set; } = 0;

        public int CsHtmlLinesCount { get; set; } = 0;

        public int AshxLinesCount { get; set; } = 0;

        public int AscxLinesCount { get; set; } = 0;

        public List<CSharpSolution> Solutions { get; set; } = new List<CSharpSolution>();

        public CSharpStatistics()
        {
        }
    }
}
