using System.IO;
using System.Text;
using LelCommon;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Vibrant.InfluxDB.Client;
using System;
using System.Collections.Generic;

namespace LelConsumer
{
    public class Program
    {
        private static IMongoClient _mongoClient;
        private static IMongoDatabase _database;
        private static IModel _channel;
        private static InfluxClient _client;
        private static int _counter;
        private static List<ConsumerInfo> _infoToSend = new List<ConsumerInfo>();
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            _client = new InfluxClient(new Uri(configuration["influxConnectionString"]));
            _client.CreateDatabaseAsync("statsDb").Wait();
            _mongoClient = new MongoClient(configuration["mongoConnectionString"]);
            _database = _mongoClient.GetDatabase("lel");
            var factory = new ConnectionFactory { HostName = configuration["rabbitmq:address"], Port = int.Parse(configuration["rabbitmq:port"]), UserName = configuration["rabbitmq:user"], Password = configuration["rabbitmq:password"] };
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
            var result = JsonConvert.DeserializeObject<Result>(Encoding.UTF8.GetString(ea.Body));
            _database.GetCollection<Result>("lels").InsertOne(result);
            _channel.BasicPublish("lel_stored", "lel_stored", null, ea.Body);
            _channel.BasicAck(ea.DeliveryTag, false);

            _infoToSend.Add(new ConsumerInfo { Timestamp = DateTime.Now.AddHours(-1), Configuration = result.Configuration });
            _counter++;
            if (_counter >= 100)
            {
                _counter = 0;
                _client.WriteAsync("statsDb", "consumerStats", _infoToSend).Wait();
                _infoToSend.Clear();
            }


        }
    }
}
