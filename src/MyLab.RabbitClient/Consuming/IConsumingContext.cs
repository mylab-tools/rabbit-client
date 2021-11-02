using System;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Sets and release consuming context
    /// </summary>
    public interface IConsumingContext 
    {
        /// <summary>
        /// Set context
        /// </summary>
        public IDisposable Set(BasicDeliverEventArgs deliverEventArgs);
    }
}
