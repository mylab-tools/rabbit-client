using System;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Set context for publishing message
    /// </summary>
    public interface IPublishingContext
    {
        /// <summary>
        /// Set context
        /// </summary>
        IDisposable Set(RabbitPublishingMessage publishingMessage);
    }
}
