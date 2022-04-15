using System;
using System.Threading.Tasks;
using MyLab.RabbitClient.Connection;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Customize publisher builder logic
    /// </summary>
    public interface IPublisherBuilderStrategy
    {
        /// <summary>
        /// Uses strategy logic
        /// </summary>
        void Use(Action<IPublisherBuilderStrategyUsing> use);
    }

    /// <summary>
    /// Custom publisher builder logic part
    /// </summary>
    public interface IPublisherBuilderStrategyUsing
    {
        /// <summary>
        /// Creates basic Rabbit properties
        /// </summary>
        IBasicProperties CreateBasicProperties();

        /// <summary>
        /// Publishes a message
        /// </summary>
        void Publish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] content);
    }

    class ChannelBasedPublisherBuilderStrategy : IPublisherBuilderStrategy
    {
        private readonly IRabbitChannelProvider _channelProvider;

        public ChannelBasedPublisherBuilderStrategy(IRabbitChannelProvider channelProvider)
        {
            _channelProvider = channelProvider;
        }

        public void Use(Action<IPublisherBuilderStrategyUsing> use)
        {
            _channelProvider.Use(model =>
            {
                var @using = new ChannelBasedPublisherBuilderStrategyUsing(model);

                use(@using);
            });
        }
    }

    class ChannelBasedPublisherBuilderStrategyUsing : IPublisherBuilderStrategyUsing
    {
        private readonly IModel _channel;

        public ChannelBasedPublisherBuilderStrategyUsing(IModel channel)
        {
            _channel = channel;
        }

        public IBasicProperties CreateBasicProperties()
        {
            return _channel.CreateBasicProperties();
        }

        public void Publish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] content)
        {
            _channel.BasicPublish(
                exchange ?? "",
                routingKey ?? "",
                basicProperties,
                content
            );
        }
    }
}