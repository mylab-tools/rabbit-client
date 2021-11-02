using RabbitMQ.Client;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Publishing message parameters
    /// </summary>
    public class RabbitPublishingMessage
    {
        /// <summary>
        /// Basic properties
        /// </summary>
        public IBasicProperties BasicProperties { get; set; }
        /// <summary>
        /// Binary content
        /// </summary>
        public byte[] Content { get; set; }
        /// <summary>
        /// Routing key or queue name
        /// </summary>
        public string RoutingKey { get; set; }
        /// <summary>
        /// Exchange name
        /// </summary>
        public string Exchange { get; set; }
    }
}