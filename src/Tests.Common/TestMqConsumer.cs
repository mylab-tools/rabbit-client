using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MyLab.Mq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tests.Common
{
    public class TestMqConsumer
    {
        private readonly string _queue;

        public TestMqConsumer(string queue)
        {
            _queue = queue;
        }

        public MqMessage<T> Listen<T>()
            where T : class
        {
            var factory = TestQueue.CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var consumeBlock = new AutoResetEvent(false);

            MqMessage<T> result = null;
            Exception e = null;

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    var payload = JsonConvert.DeserializeObject<T>(message);

                    result = new MqMessage<T>(payload)
                    {
                        ReplyTo = !string.IsNullOrEmpty(ea.BasicProperties.ReplyTo) 
                            ? new PublishTarget(ea.BasicProperties.ReplyToAddress)
                            : null,
                        CorrelationId = !string.IsNullOrEmpty(ea.BasicProperties.CorrelationId)
                            ? new Guid(ea.BasicProperties.CorrelationId)
                            : Guid.Empty,
                        MessageId = !string.IsNullOrEmpty(ea.BasicProperties.MessageId)
                            ? new Guid(ea.BasicProperties.MessageId)
                            : Guid.Empty
                    };

                    if (ea.BasicProperties.Headers != null)
                    {
                        result.Headers = ea.BasicProperties.Headers
                            .Select(MqHeader.Create)
                            .ToArray();
                    }
                }
                catch (Exception exception)
                {
                    e = exception;
                }
                finally
                {
                    consumeBlock.Set();
                }
            };

            channel.BasicConsume(queue: _queue, consumer: consumer);

            if (!consumeBlock.WaitOne(TimeSpan.FromSeconds(1)))
                throw new TimeoutException();

            if (e != null)
                throw new TargetInvocationException(e);

            return result;
        }
    }
}