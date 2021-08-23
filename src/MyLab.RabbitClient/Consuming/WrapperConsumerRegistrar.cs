using System;
using Microsoft.Extensions.DependencyInjection;

namespace MyLab.RabbitClient.Consuming
{
    class WrapperConsumerRegistrar<TRegistrar> : IRabbitConsumerRegistrar
        where TRegistrar : class, IRabbitConsumerRegistrar
    {
        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            var registrar = ActivatorUtilities.CreateInstance<TRegistrar>(serviceProvider);

            registrar.Register(registry, serviceProvider);
        }
    }
}