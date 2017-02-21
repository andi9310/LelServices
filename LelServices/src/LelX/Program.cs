using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace LelX
{
    public class Program
    {
        private static ConnectionFactory _factory;
        public static IConnection Connection { get; private set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                       .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json");

            var configuration = builder.Build();
            _factory = new ConnectionFactory { HostName = configuration["rabbitmq:address"], Port = int.Parse(configuration["rabbitmq:port"]), UserName = configuration["rabbitmq:user"], Password = configuration["rabbitmq:password"] };
            Connection = _factory.CreateConnection();

            using (var channel = Connection.CreateModel())
            {
                channel.QueueDeclare("lel_new", true, false, false, null);
            }
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
            Connection.Dispose();
        }
    }
}
