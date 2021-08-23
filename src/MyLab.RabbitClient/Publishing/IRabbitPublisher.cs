namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Provides abilities to publish messages
    /// </summary>
    public interface IRabbitPublisher
    {
        /// <summary>
        /// Create publish builder for target from default publish options
        /// </summary>
        RabbitPublisherBuilder IntoDefault(string routingKey = null);
        /// <summary>
        /// Create publish builder for specified queue
        /// </summary>
        RabbitPublisherBuilder IntoQueue(string queue);
        /// <summary>
        /// Create publish builder for specified exchange
        /// </summary>
        RabbitPublisherBuilder IntoExchange(string exchange, string routingKey = null);
        /// <summary>
        /// Create publish builder for target from model referenced options
        /// </summary>
        RabbitPublisherBuilder Into<TMsg>(string routingKey = null);
        /// <summary>
        /// Create publish builder for target from referenced options
        /// </summary>
        RabbitPublisherBuilder Into(string configId, string routingKey = null);
    }
}