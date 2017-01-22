using System.IO;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;

namespace LelApi
{
    public class Program
    {
        private static readonly IMongoClient MongoClient = new MongoClient("mongodb://localhost:8007/?maxPoolSize=555");
        public static IMongoDatabase Database { get; } = MongoClient.GetDatabase("lel");
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
