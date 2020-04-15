using System;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IntegrationTests.Tools
{
    static class TestMqConsumer
    {
        public static T Listen<T>()
            where T : class
        {
            var factory = TestQueue.CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var consumeBlock = new AutoResetEvent(false);

            T result = null;

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);

                result = JsonConvert.DeserializeObject<T>(message);

                consumeBlock.Set();
            };

            channel.BasicConsume(queue: TestQueue.Name, consumer: consumer);

            consumeBlock.WaitOne(TimeSpan.FromSeconds(5));

            return result;
        }
    }
}