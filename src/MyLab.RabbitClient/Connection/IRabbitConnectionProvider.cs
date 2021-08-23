using System;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Provides connection to RabbitMQ
    /// </summary>
    public interface IRabbitConnectionProvider
    {
        /// <summary>
        /// Occurs when new connection created 
        /// </summary>
        /// <remarks>
        /// It means that old connection no longer available
        /// </remarks>
        event EventHandler Reconnected;

        /// <summary>
        /// Provides Mq connection
        /// </summary>
        IConnection Provide();
    }
}
