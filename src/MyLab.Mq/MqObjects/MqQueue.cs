using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyLab.Mq.Communication;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Represent MQ queue
    /// </summary>
    public class MqQueue : IDisposable
    {
        private readonly MqChannelProvider _channelProvider;

        /// <summary>
        /// Queue name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqQueue"/>
        /// </summary>
        public MqQueue(string name, IMqConnectionProvider connectionProvider)
        {
            _channelProvider = new MqChannelProvider(connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider)));
            Name = name;
        }

        /// <summary>
        /// Publish object as JSON message content
        /// </summary>
        public void Publish(object message)
        {
            string messageStr = JsonConvert.SerializeObject(message);
            var messageBin = Encoding.UTF8.GetBytes(messageStr);

            _channelProvider.Provide().BasicPublish(
                exchange: string.Empty,
                routingKey: Name,
                body: messageBin);
        }


        /// <summary>
        /// Listens next message synchronously 
        /// </summary>
        public MqMessageRead<T> Listen<T>(TimeSpan? timeout = null)
            where T : class
        {
            return ListenCore<T>(timeout, false);
        }

        /// <summary>
        /// Listens next message synchronously 
        /// </summary>
        public MqMessage<T> ListenAutoAck<T>(TimeSpan? timeout = null)
            where T : class
        {
            return ListenCore<T>(timeout, true).Message;
        }

        MqMessageRead<T> ListenCore<T>(TimeSpan? timeout, bool autoAck)
            where T : class
        {
            var channel = _channelProvider.Provide();

            var consumeBlock = new AutoResetEvent(false);

            MqMessage<T> result = null;
            Exception e = null;

            ulong deliveryTag = 0;
            
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

                    deliveryTag = ea.DeliveryTag;
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

            channel.BasicConsume(queue: Name, consumer: consumer, autoAck: autoAck);

            if (!consumeBlock.WaitOne(timeout ?? TimeSpan.FromSeconds(1)))
                throw new TimeoutException();

            if (e != null)
                throw new TargetInvocationException(e);

            return new MqMessageRead<T>(consumer.Model, deliveryTag, result);
        }

        /// <summary>
        /// Binds queue to exchange
        /// </summary>
        public void BindToExchange(string exchangeName, string routingKey = null)
        {
            _channelProvider.Provide().QueueBind(Name, exchangeName, routingKey ?? "");
        }

        /// <summary>
        /// Binds queue to exchange
        /// </summary>
        public void BindToExchange(MqExchange exchange, string routingKey = null)
        {
            BindToExchange(exchange.Name, routingKey);
        }

        /// <summary>
        /// Gets 'true' if queue exists 
        /// </summary>
        /// <returns></returns>
        public bool IsExists()
        {
            try
            {
                _channelProvider.Provide().QueueDeclarePassive(Name);
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyText.Contains("no queue"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove Queue
        /// </summary>
        public void Dispose()
        {
            _channelProvider.Provide().QueueDelete(Name, false, false);
        }
    }
}