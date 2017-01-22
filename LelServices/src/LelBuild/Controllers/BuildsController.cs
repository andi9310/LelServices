using System;
using Microsoft.AspNetCore.Mvc;
using LelCommon;

namespace LelBuild.Controllers
{
    [Route("api/[controller]")]
    public class BuildsController : Controller
    {
        // GET api/builds/5/last
        [HttpGet("{configurationId}/last")]
        public Build Get(int configurationId)
        {
            return new Build { Configuration = configurationId, Label = $"1.0.{Guid.NewGuid()}" };
        }
    }
}
