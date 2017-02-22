using LelCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;

namespace LelFlow
{
    public class Program
    {
        private static HttpClient _buildClient;
        private static HttpClient _runnerClient;
        private static readonly Random Generator = new Random();
        public static void Main(string[] args)
        {
            Main().Wait();
        }

        private static async Task Main()
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            _buildClient = new HttpClient { BaseAddress = new Uri(configuration["lelBuildConnectionString"]) };
            _runnerClient = new HttpClient { BaseAddress = new Uri(configuration["lelXConnectionString"]) };

            using (var client = new ConsulClient(consulConfig => consulConfig.Address = new Uri(configuration["consulConnectionString"])))
            {

                const string keyName = "lel/lel_flow_lock";

                var lockOptions = new LockOptions(keyName)
                {
                    SessionName = "lel_lock_session",
                    SessionTTL = TimeSpan.FromSeconds(10)
                };

                var l = client.CreateLock(lockOptions);
                await l.Acquire(CancellationToken.None);

                try
                {
                    if (!l.IsHeld)
                    {
                        Console.WriteLine("dziwne");
                        return;
                    }
                    Console.WriteLine("mam locka");

                    _buildClient.DefaultRequestHeaders.Accept.Clear();
                    _buildClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    while (true)
                    {
                        ProcessBuild().Wait();
                        Thread.Sleep(5000);
                    }
                }
                finally
                {
                    await l.Release(CancellationToken.None);
                }
            }
        }

        private static async Task ProcessBuild()
        {
            var response = await _buildClient.GetAsync($"api/builds/{Generator.Next(100)}/last");
            if (response.IsSuccessStatusCode)
            {
                await _runnerClient.PostAsJsonAsync("api/tests", AddTestsToBuild(await response.Content.ReadAsAsync<Build>()));
            }
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
