using System.Collections.Generic;

namespace LelCommon
{
    public class Build
    {
        public string Label { get; set; }
        public int Configuration { get; set; }
    }

    public class Test
    {
        public string Command { get; set; }
    }

    public class BuildWithTests : Build
    {
        public IEnumerable<Test> Tests { get; set; }

    }

    public class Result : Build
    {
        public string Command { get; set; }
        public int Status { get; set; }
    }


}
