using System.Collections.Generic;
using LelCommon;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;


namespace LelApi.Controllers
{
    [Route("api/[controller]")]
    public class BuildsController : Controller
    {
        // GET api/builds/configuration/2
        [HttpGet("configuration/{configurationId}")]
        public IEnumerable<string> Get(int configurationId)
        {
            return Program.Database.GetCollection<Result>("lels").Distinct<string>("Label", Builders<Result>.Filter.Eq("Configuration", configurationId)).ToList();
        }

    }
}
