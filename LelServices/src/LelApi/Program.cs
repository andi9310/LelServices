using System.Data.SqlClient;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace LelApi
{
    public class Program
    {
        private static IMongoClient _mongoClient;
        public static IMongoDatabase Database { get; private set; }
        public static string SqlConnectionString { get; private set; }
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                         .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json");

            var configuration = builder.Build();
            _mongoClient = new MongoClient(configuration["mongoConnectionString"]);
            Database = _mongoClient.GetDatabase("lel");
            SqlConnectionString = configuration["sqlConnectionString"];

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
