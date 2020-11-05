namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Registers MQ consumers
    /// </summary>
    public interface IMqConsumerRegistrar
    {
        /// <summary>
        /// Registers MQ consumer
        /// </summary>
        void RegisterConsumer(MqConsumer consumer);
    }
}