namespace MyLab.MqApp
{
    /// <summary>
    /// Manages consumers and incoming messages
    /// </summary>
    public interface IMqConsumerManager
    {

    }

    /// <summary>
    /// Registers MQ consumers
    /// </summary>
    public interface IMqConsumerRegistrar
    {
        /// <summary>
        /// Registers MQ consumer
        /// </summary>
        void Register(string queueName, IMqConsumer consumer);
    }
}