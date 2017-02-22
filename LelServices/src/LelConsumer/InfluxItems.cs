using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vibrant.InfluxDB.Client;

namespace LelConsumer
{
    public class ConsumerInfo
    {
        [InfluxTimestamp]
        public DateTime Timestamp { get; set; }


        [InfluxField("configuration")]
        public long Configuration { get; set; }
    }
}
