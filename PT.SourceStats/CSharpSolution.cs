using System.Collections.Generic;

namespace PT.SourceStats
{
    public class CSharpSolution
    {
        public string Name { get; set; }

        public List<CSharpProject> Projects { get; set; } = new List<CSharpProject>();

        public CSharpSolution()
        {

        }
    }
}
