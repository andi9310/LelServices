using LelCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LelFlow
{
    public class Program
    {
        private static readonly HttpClient BuildClient = new HttpClient { BaseAddress = new Uri("http://localhost:3296/") };
        private static readonly HttpClient RunnerClient = new HttpClient { BaseAddress = new Uri("http://localhost:3318/") };
        private static readonly Random Generator = new Random();
        public static void Main(string[] args)
        {
            BuildClient.DefaultRequestHeaders.Accept.Clear();
            BuildClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            while (true)
            {
                ProcessBuild().Wait();
                System.Threading.Thread.Sleep(5000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static async Task ProcessBuild()
        {
            var response = await BuildClient.GetAsync($"api/builds/{Generator.Next(100)}/last");
            if (response.IsSuccessStatusCode)
            {
                var build = await response.Content.ReadAsAsync<Build>();
                var buildwithTests = AddTestsToBuild(build);
                SendToExecution(buildwithTests);
            }
        }

        private static void SendToExecution(BuildWithTests build)
        {
            RunnerClient.PostAsJsonAsync("api/tests", build);
        }

        private static BuildWithTests AddTestsToBuild(Build build)
        {
            var tests = new List<Test>();
            var generator = new Random(build.Configuration);
            for (var i = 0; i < build.Configuration * 100; i++)
            {
                tests.Add(new Test() { Command = RandomString(generator) });
            }
            return new BuildWithTests { Configuration = build.Configuration, Label = build.Label, Tests = tests };
        }

        private static string RandomString(Random generator)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, 3).Select(s => s[generator.Next(s.Length)]).ToArray());
        }

    }
}
