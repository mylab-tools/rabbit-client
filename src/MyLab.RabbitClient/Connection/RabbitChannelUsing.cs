using System;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Contains RabbitMQ channel
    /// </summary>
    public class RabbitChannelUsing : IDisposable
    {
        private readonly IDisposable _disposer;

        /// <summary>
        /// Rabbit channel
        /// </summary>
        public IModel Channel { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitChannelUsing"/>
        /// </summary>
        public RabbitChannelUsing(IModel channel, IDisposable disposer)
        {
            _disposer = disposer;
            Channel = channel;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _disposer.Dispose();
        }
    }
}