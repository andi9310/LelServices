using LelCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Consul;

namespace LelFlow
{
    public class Program
    {
        private static readonly HttpClient BuildClient = new HttpClient { BaseAddress = new Uri("http://lelbuild/") };
        private static readonly HttpClient RunnerClient = new HttpClient { BaseAddress = new Uri("http://lelx/") };
        private static readonly Random Generator = new Random();
        public static void Main(string[] args)
        {
            Main().Wait();
        }

        private static async Task Main()
        {
            using (var client = new ConsulClient(configuration => configuration.Address = new Uri("http://consul:8500")))
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

                    BuildClient.DefaultRequestHeaders.Accept.Clear();
                    BuildClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
            var response = await BuildClient.GetAsync($"api/builds/{Generator.Next(100)}/last");
            if (response.IsSuccessStatusCode)
            {
                await RunnerClient.PostAsJsonAsync("api/tests", AddTestsToBuild(await response.Content.ReadAsAsync<Build>()));
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
