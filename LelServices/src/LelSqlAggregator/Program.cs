using System.Linq;
using System.Reflection;
using System.Text;
using LelCommon;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LelSqlAggregator
{
    public class Program
    {
        private static IModel _channel;
        public static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq", Port = 5672, UserName = "guest", Password = "guest" };
            using (var connection = factory.CreateConnection())
            using (_channel = connection.CreateModel())
            {
                _channel.BasicQos(0, 100, false);
                _channel.QueueDeclare("lel_stored_sql_agg", true, false, false, null);
                _channel.ExchangeDeclare("lel_stored", "fanout");
                _channel.QueueBind("lel_stored_sql_agg", "lel_stored", "");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += OnMessage;
                _channel.BasicConsume("lel_stored_sql_agg", false, consumer);

                while (true)
                {
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static void OnMessage(object model, BasicDeliverEventArgs ea)
        {
            var result = JsonConvert.DeserializeObject<Result>(Encoding.UTF8.GetString(ea.Body));
            using (var context = new LelContext(new DbContextOptions<LelContext>()))
            {
                context.Database.EnsureCreated();

                var aggregation = context.Aggregations.FirstOrDefault(agg => agg.Command == result.Command);
                if (aggregation == null)
                {
                    aggregation = new Aggregation
                    {
                        Command = result.Command,
                        Pass = 0,
                        Fail = 0,
                        Error = 0
                    };
                    context.Aggregations.Add(aggregation);
                }
                var status = aggregation.GetType().GetProperty(result.Status);
                status.SetValue(aggregation, (int)status.GetValue(aggregation, null) + 1);
                context.SaveChanges();
            }
            _channel.BasicAck(ea.DeliveryTag, false);
        }
    }
}

