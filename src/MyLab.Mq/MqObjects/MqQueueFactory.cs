using System;
using MyLab.Mq.Communication;

namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Creates queue
    /// </summary>
    public class MqQueueFactory : MqQueueFactoryBase
    {
        private readonly IMqChannelProvider _channelProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MqQueueFactory"/>
        /// </summary>
        public MqQueueFactory(IMqChannelProvider channelProvider)
        {
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        }


        /// <inheritdoc />
        protected override IMqChannelProvider GetChannelProvider()
        {
            return _channelProvider;
        }
    }
}
