using System;
using RabbitMQ.Client;

namespace MyLab.Mq.Communication
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