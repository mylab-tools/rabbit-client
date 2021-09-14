namespace MyLab.RabbitClient
{
    /// <summary>
    /// Rabbit connection establish strategy
    /// </summary>
    public enum RabbitConnectionStrategy
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined,
        /// <summary>
        /// When requested
        /// </summary>
        Lazy,
        /// <summary>
        /// In background when application started
        /// </summary>
        Background
    }
}