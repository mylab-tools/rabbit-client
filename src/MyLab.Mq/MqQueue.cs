using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq
{
    /// <summary>
    /// Represent MQ queue
    /// </summary>
    public class MqQueue : IDisposable
    {
        private readonly IMqConnectionProvider _connectionProvider;

        /// <summary>
        /// Queue name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqQueue"/>
        /// </summary>
        public MqQueue(string name, IMqConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            Name = name;
        }

        /// <summary>
        /// Publish object as JSON message content
        /// </summary>
        public void Publish(object message)
        {
            using var ch = new MqChannelProvider(_connectionProvider);
            
            string messageStr = JsonConvert.SerializeObject(message);
            var messageBin = Encoding.UTF8.GetBytes(messageStr);

            ch.Provide().BasicPublish(
                exchange: string.Empty,
                routingKey: Name,
                body: messageBin);
        }

        /// <summary>
        /// Listens next message synchronously 
        /// </summary>
        public MqMessage<T> Listen<T>(TimeSpan? timeout = null)
            where T : class
        {
            using var chProvider = new MqChannelProvider(_connectionProvider);

            var channel = chProvider.Provide();

            var consumeBlock = new AutoResetEvent(false);

            MqMessage<T> result = null;
            Exception e = null;

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    var payload = JsonConvert.DeserializeObject<T>(message);

                    result = new MqMessage<T>(payload)
                    {
                        ReplyTo = ea.BasicProperties.ReplyTo,
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

                return Task.CompletedTask;
            };

            channel.BasicConsume(queue: Name, consumer: consumer);

            if (!consumeBlock.WaitOne(timeout ?? TimeSpan.FromSeconds(1)))
                throw new TimeoutException();

            if (e != null)
                throw new TargetInvocationException(e);

            return result;
        }

        /// <summary>
        /// Remove Queue
        /// </summary>
        public void Dispose()
        {
            using var channelProvider = new MqChannelProvider(_connectionProvider);
            channelProvider.Provide().QueueDelete(Name, false, false);
        }
    }
}