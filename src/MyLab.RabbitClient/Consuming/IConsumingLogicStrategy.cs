using RabbitMQ.Client;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Receives an acknowledgment response to the received message
    /// </summary>
    public interface IConsumingLogicStrategy
    {
        /// <summary>
        /// Acknowledgement 
        /// </summary>
        void Ack(ulong deliveryTag);

        /// <summary>
        /// Negative acknowledgement
        /// </summary>
        void Nack(ulong deliveryTag);
    }

    class DefaultConsumingLogicStrategy : IConsumingLogicStrategy
    {
        private readonly IModel _channel;

        public DefaultConsumingLogicStrategy(IModel channel)
        {
            _channel = channel;
        }

        public void Ack(ulong deliveryTag)
        {
            _channel.BasicAck(deliveryTag, false);
        }

        public void Nack(ulong deliveryTag)
        {
            _channel.BasicNack(deliveryTag, false, false);
        }
    }
}