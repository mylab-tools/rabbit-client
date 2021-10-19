using RabbitMQ.Client;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Processes message before publishing
    /// </summary>
    public interface IPublishingMessageProcessor
    {
        /// <summary>
        /// Process a message
        /// </summary>
        void Process(IBasicProperties basicProperties, ref byte[] content);
    }
}
