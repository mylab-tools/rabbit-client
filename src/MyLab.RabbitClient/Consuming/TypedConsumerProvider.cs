using System;
using Microsoft.Extensions.DependencyInjection;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Creates consumer instance with <see cref="IServiceProvider"/> and provides it
    /// </summary>
    public class TypedConsumerProvider<TConsumer> : IRabbitConsumerProvider
        where TConsumer : class, IRabbitConsumer
    {
        private readonly object[] _emptyArgs = new object[0];
        private readonly object[] _ctorArgs;

        /// <summary>
        /// Initializes a new instance of <see cref="TypedConsumerProvider{T}"/>
        /// </summary>
        public TypedConsumerProvider(object[] ctorArgs = null)
        {
            _ctorArgs = ctorArgs ?? _emptyArgs;
        }


        /// <inheritdoc />
        public IRabbitConsumer Provide(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<TConsumer>(serviceProvider, _ctorArgs);
        }
    }
}