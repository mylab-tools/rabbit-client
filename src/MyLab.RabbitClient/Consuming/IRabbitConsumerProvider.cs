using System;
using Microsoft.VisualBasic;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Provides consumer
    /// </summary>
    public interface IRabbitConsumerProvider
    {
        /// <summary>
        /// Provides consumer logic
        /// </summary>
        IRabbitConsumer Provide(IServiceProvider serviceProvider);
    }
}