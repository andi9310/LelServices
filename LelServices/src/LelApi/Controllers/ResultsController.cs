using System.Collections.Generic;
using LelCommon;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;


namespace LelApi.Controllers
{
    [Route("api/[controller]")]
    public class ResultsController : Controller
    {
        // GET api/results/configuration/2/build/1.0.1
        [HttpGet("configuration/{configurationId}/build/{buildLabel}")]
        public IEnumerable<Result> Get(int configurationId, string buildLabel)
        {
            var filterBuilder = Builders<Result>.Filter;
            return Program.Database.GetCollection<Result>("lels").FindSync(filterBuilder.Eq("Label", buildLabel) & filterBuilder.Eq("Configuration", configurationId)).ToList();
        }

    }
}
