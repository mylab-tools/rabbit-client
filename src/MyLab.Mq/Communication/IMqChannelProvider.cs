using System;
using RabbitMQ.Client;

namespace MyLab.Mq.Communication
{
    /// <summary>
    /// Provides MQ channels
    /// </summary>
    public interface IMqChannelProvider
    {
        /// <summary>
        /// Provides MQ channel with specified prefetch count
        /// </summary>
        IModel Provide(ushort prefetchCount = 1);
    }

    /// <summary>
    /// Represent MQ channel
    /// </summary>
    /// <remarks>Should be disposed after using</remarks>
    public class MqChannel : IDisposable
    {
        private readonly Action<IModel> _disposer;
        
        /// <summary>
        /// Mq channel model
        /// </summary>
        public IModel Model { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqChannel"/>
        /// </summary>
        public MqChannel(IModel model, Action<IModel> disposer = null)
        {
            _disposer = disposer;
            Model = model;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _disposer(Model);
        }
    }
}