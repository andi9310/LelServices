using System.IO;
using Microsoft.AspNetCore.Hosting;
using RabbitMQ.Client;

namespace LelX
{
    public class Program
    {
        private static readonly ConnectionFactory Factory = new ConnectionFactory { HostName = "localhost", Port = 8006, UserName = "guest", Password = "guest" };
        public static IConnection Connection { get; } = Factory.CreateConnection();

        public static void Main(string[] args)
        {
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
