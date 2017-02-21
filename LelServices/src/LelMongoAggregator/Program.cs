using System.IO;
using System.Text;
using LelCommon;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LelMongoAggregator
{
    public class Program
    {
        private static IMongoClient _mongoClient;
        private static IMongoDatabase _database;
        private static IModel _channel;
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json");

            var configuration = builder.Build();
            _mongoClient = new MongoClient(configuration["mongoConnectionString"]);
            _database = _mongoClient.GetDatabase("lel");
            var factory = new ConnectionFactory { HostName = configuration["rabbitmq:address"], Port = int.Parse(configuration["rabbitmq:port"]), UserName = configuration["rabbitmq:user"], Password = configuration["rabbitmq:password"] };
            using (var connection = factory.CreateConnection())
            using (_channel = connection.CreateModel())
            {
                _channel.BasicQos(0, 100, false);
                _channel.QueueDeclare("lel_stored_mongo_agg", true, false, false, null);
                _channel.ExchangeDeclare("lel_stored", "fanout");
                _channel.QueueBind("lel_stored_mongo_agg", "lel_stored", "");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += OnMessage;
                _channel.BasicConsume("lel_stored_mongo_agg", false, consumer);

                while (true)
                {
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static void OnMessage(object model, BasicDeliverEventArgs ea)
        {
            var result = JsonConvert.DeserializeObject<Result>(Encoding.UTF8.GetString(ea.Body));
            var filterBuilder = Builders<MongoAggregation>.Filter;
            _database.GetCollection<MongoAggregation>("lel_aggregations").FindOneAndUpdate(
                filterBuilder.Eq("Label", result.Label) & filterBuilder.Eq("Configuration", result.Configuration),
                Builders<MongoAggregation>.Update.Inc(result.Status, 1),
                new FindOneAndUpdateOptions<MongoAggregation, MongoAggregation> { IsUpsert = true });
            _channel.BasicAck(ea.DeliveryTag, false);
        }
    }
}
