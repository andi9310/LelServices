using System;
using System.Collections.Generic;
using System.Linq;
using LelBuild.Models;
using Microsoft.AspNetCore.Mvc;

namespace LelBuild.Controllers
{
    [Route("api/[controller]")]
    public class BuildsController : Controller
    {
        // GET api/builds/5/1.0.1
        [HttpGet("{configurationId}/{buildLabel}")]
        public Build Get(int configurationId, string buildLabel)
        {
            var tests = new List<Test>();
            var generator = new Random(configurationId);
            for (var i = 0; i < configurationId * 1000; i++)
            {
                tests.Add(new Test() { Command = RandomString(generator) });
            }
            return new Build { Configuration = configurationId, Label = buildLabel, Tests = tests };
        }
        private static string RandomString(Random generator)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, 3).Select(s => s[generator.Next(s.Length)]).ToArray());
        }
    }
}
