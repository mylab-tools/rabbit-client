namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Registers initial MQ consumers
    /// </summary>
    public interface IMqInitialConsumerRegistrar
    {
        /// <summary>
        /// Registers MQ consumer
        /// </summary>
        void RegisterConsumer(MqConsumer consumer);
    }
}