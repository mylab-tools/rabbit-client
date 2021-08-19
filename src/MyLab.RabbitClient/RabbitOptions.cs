namespace MyLab.RabbitClient
{
    /// <summary>
    /// Contains connection options
    /// </summary>
    public class RabbitOptions
    {
        /// <summary>
        /// Server host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Virtual host
        /// </summary>
        public string VHost { get; set; }

        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; set; } = 5672;
        /// <summary>
        /// Login user
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Login password
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Publish options
        /// </summary>
        public IdPublishOptions[] Pub { get; set; }
        /// <summary>
        /// Default publish options
        /// </summary>
        public PublishOptions DefaultPub { get; set; }
    }

    /// <summary>
    /// Contains publish options
    /// </summary>
    public class PublishOptions
    {
        /// <summary>
        /// Target exchange
        /// </summary>
        public string Exchange { get; set; }
        /// <summary>
        /// Routing key or Queue if Exchange is empty
        /// </summary>
        public string RoutingKey { get; set; }
    }

    /// <summary>
    /// Contains publish options with identifier
    /// </summary>
    public class IdPublishOptions : PublishOptions
    {
        /// <summary>
        /// Publish options identifier
        /// </summary>
        public string Id { get; set; }
    }
}
