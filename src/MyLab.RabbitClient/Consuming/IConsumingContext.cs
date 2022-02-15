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
        public IConsumingContextInstance Set(BasicDeliverEventArgs deliverEventArgs);
    }

    /// <summary>
    /// Represents established consuming context instance
    /// </summary>
    public interface IConsumingContextInstance : IDisposable
    {
        /// <summary>
        /// Notifies about unhandled exception which thrown in this context
        /// </summary>
        void NotifyUnhandledException(Exception exception);
    }
}
