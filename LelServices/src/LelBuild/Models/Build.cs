using System.Collections.Generic;

namespace LelBuild.Models
{
    public class Build
    {
        public string Label { get; set; }
        public int Configuration { get; set; }
        public IEnumerable<Test> Tests { get; set; } 
    }

    public class Test
    {
        public string Command { get; set; }
    }
}
