using System.Text;
using LelCommon;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LelConsumer
{
    public class Program
    {
        private static readonly IMongoClient MongoClient = new MongoClient("mongodb://localhost:8007/?maxPoolSize=555");
        private static readonly IMongoDatabase Database = MongoClient.GetDatabase("lel");
        private static IModel _channel;
        public static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost", Port = 8006, UserName = "guest", Password = "guest" };
            using (var connection = factory.CreateConnection())
            using (_channel = connection.CreateModel())
            {
                _channel.BasicQos(0, 100, false);
                _channel.QueueDeclare("lel_new", true, false, false, null);
                _channel.ExchangeDeclare("lel_stored", "fanout");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += OnMessage;
                _channel.BasicConsume("lel_new", false, consumer);

                while (true)
                {
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static void OnMessage(object model, BasicDeliverEventArgs ea)
        {
            Database.GetCollection<Result>("lels").InsertOne(JsonConvert.DeserializeObject<Result>(Encoding.UTF8.GetString(ea.Body)));
            _channel.BasicPublish("lel_stored", "lel_stored", null, ea.Body);
            _channel.BasicAck(ea.DeliveryTag, false);
        }
    }
}
