using System;
using System.Text;
using LelCommon;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace LelX.Controllers
{
    [Route("api/[controller]")]
    public class TestsController : Controller
    {
        private static readonly Random Generator = new Random();

        private static readonly string[] Statuses = { "Pass", "Fail", "Error" };
        // POST api/tests
        [HttpPost]
        public void Post([FromBody]BuildWithTests build)
        {
            using (var channel = Program.Connection.CreateModel())
            {
                foreach (var test in build.Tests)
                {
                    channel.BasicPublish("", "lel_new", null,
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                new Result
                                {
                                    Configuration = build.Configuration,
                                    Label = build.Label,
                                    Command = test.Command,
                                    Status = Statuses[Generator.Next(Statuses.Length)]
                                }
                            )
                        )
                   );
                }
            }
        }
    }
}
