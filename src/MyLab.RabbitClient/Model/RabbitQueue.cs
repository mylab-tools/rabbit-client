using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Consuming;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MyLab.RabbitClient.Model
{
    /// <summary>
    /// Represent Rabbit queue
    /// </summary>
    public class RabbitQueue 
    {
        private readonly IRabbitChannelProvider _channelProvider;

        private RabbitChannelUsing _listenChannel;

        /// <summary>
        /// Queue name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitQueue"/>
        /// </summary>
        public RabbitQueue(string queue, IRabbitChannelProvider channelProvider)
        {
            Name = queue ?? throw new ArgumentNullException(nameof(queue));
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        }

        /// <summary>
        /// Publish object as JSON message content
        /// </summary>
        public void Publish(object message, IBasicProperties basicProperties = null)
        {
            string messageStr = JsonConvert.SerializeObject(message);
            var messageBin = Encoding.UTF8.GetBytes(messageStr);

            _channelProvider.Use(ch => ch.BasicPublish(
                exchange: string.Empty,
                routingKey: Name,
                body: messageBin,
                basicProperties: basicProperties)
            );
        }

        /// <summary>
        /// Listens next message synchronously 
        /// </summary>
        public ConsumedMessage<T> Listen<T>(TimeSpan? timeout = null)
            where T : class
        {
            var consumeBlock = new AutoResetEvent(false);

            Exception e = null;
            ConsumedMessage<T> result = null;

            var chUsing = _listenChannel ??= _channelProvider.Provide();

            var consumer = new AsyncEventingBasicConsumer(chUsing.Channel);

            consumer.Received += OnConsumerOnReceived;

            var consumerTag = chUsing.Channel.BasicConsume(queue: Name, consumer: consumer, autoAck: false);

            var isTimeout = !consumeBlock.WaitOne(timeout ?? TimeSpan.FromSeconds(1));

            chUsing.Channel.BasicCancel(consumerTag);

            if (isTimeout)
                throw new TimeoutException();

            if (e != null)
                throw new TargetInvocationException(e);

            return result;

            Task OnConsumerOnReceived(object model, BasicDeliverEventArgs ea)
            {
                consumer.Received -= OnConsumerOnReceived;

                try
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    var payload = JsonConvert.DeserializeObject<T>(message);

                    result = new ConsumedMessage<T>(payload, ea);

                    //Strange hack with delivery tag. I don't know why it works.
                    ((AsyncEventingBasicConsumer)model).Model.BasicAck(ea.DeliveryTag-1, false);
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
            }
        }

        /// <summary>
        /// Binds queue to exchange
        /// </summary>
        public void BindToExchange(string exchangeName, string routingKey = null)
        {
            _channelProvider.Use(ch => ch.QueueBind(Name, exchangeName, routingKey ?? "", null));
        }

        /// <summary>
        /// Binds queue to exchange
        /// </summary>
        public void BindToExchange(RabbitExchange exchange, string routingKey = null)
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
                _channelProvider.Use(ch => ch.QueueDeclarePassive(Name));
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyText.Contains("no queue"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove queue
        /// </summary>
        public void Remove()
        {
            _listenChannel?.Dispose();
            _channelProvider.Use(ch => ch.QueueDelete(Name, false, false));
        }
    }
}
