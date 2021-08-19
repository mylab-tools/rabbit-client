using System;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Registers consumers
    /// </summary>
    public interface IRabbitConsumerRegistrar
    {
        /// <summary>
        /// Override to register consumer into consumer registry 
        /// </summary>
        void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider);
    }
}
