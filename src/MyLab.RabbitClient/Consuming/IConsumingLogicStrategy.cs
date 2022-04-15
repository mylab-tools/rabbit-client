using RabbitMQ.Client;

namespace MyLab.RabbitClient.Consuming
{
    interface IConsumingLogicStrategy
    {
        void Ack(ulong deliveryTag);
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