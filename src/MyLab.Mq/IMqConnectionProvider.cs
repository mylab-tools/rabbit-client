using System;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    /// <summary>
    /// Defines MQ connection provider
    /// </summary>
    public interface IMqConnectionProvider : IDisposable
    {
        /// <summary>
        /// Provides Mq connection
        /// </summary>
        IConnection Provide();
    }
}