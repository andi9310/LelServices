using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

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

    [BsonIgnoreExtraElements]
    public class Result : Build
    {
        public string Command { get; set; }
        public string Status { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class MongoAggregation : Build
    {
        public int Pass { get; set; }
        public int Fail { get; set; }
        public int Error { get; set; }
    }


}
