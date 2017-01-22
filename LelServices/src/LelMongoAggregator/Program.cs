﻿using System.Text;
using LelCommon;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LelMongoAggregator
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
                _channel.BasicQos(0, 20, false);
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
            var collection = Database.GetCollection<MongoAggregation>("lel_aggregations");
            var filterBuilder = Builders<MongoAggregation>.Filter;
            var filter = filterBuilder.Eq("Label", result.Label) & filterBuilder.Eq("Configuration", result.Configuration);
            var updateBuilder = Builders<MongoAggregation>.Update;
            var update = updateBuilder.Inc(result.Status, 1);
            collection.FindOneAndUpdate(filter, update, new FindOneAndUpdateOptions<MongoAggregation, MongoAggregation> { IsUpsert = true });
            _channel.BasicAck(ea.DeliveryTag, false);
        }
    }
}
