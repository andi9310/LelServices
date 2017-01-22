using System.Collections.Generic;
using System.Linq;
using LelCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;


namespace LelApi.Controllers
{
    [Route("api/[controller]")]
    public class AggregationsController : Controller
    {
        // GET api/aggregations/configuration/2
        [HttpGet("configuration/{configurationId}")]
        public IEnumerable<MongoAggregation> Get(int configurationId)
        {
            return Program.Database.GetCollection<MongoAggregation>("lel_aggregations").FindSync(Builders<MongoAggregation>.Filter.Eq("Configuration", configurationId)).ToList();
        }

        // GET api/aggregations/command/ABC
        [HttpGet("command/{command}")]
        public Aggregation Get(string command)
        {
            using (var context = new LelContext(new DbContextOptions<LelContext>()))
            {
                context.Database.EnsureCreated();
                return context.Aggregations.FirstOrDefault(agg => agg.Command == command) ?? new Aggregation { Command = command };
            }
        }

    }
}
