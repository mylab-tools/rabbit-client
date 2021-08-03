using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;

namespace MyLab.Mq.Communication
{
    /// <summary>
    /// Provides MW channels
    /// </summary>
    public class DefaultMqChannelProvider : IDisposable, IMqChannelProvider
    {
        private readonly IMqConnectionProvider _connectionProvider;
        private readonly ThreadToChannelsMap _channels = new ThreadToChannelsMap();

        /// <summary>
        /// Channels count
        /// </summary>
        public int ChannelCount => _channels.Sum(ch => ch.Value.Count);

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqChannelProvider"/>
        /// </summary>
        public DefaultMqChannelProvider(IMqConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <inheritdoc />
        public IModel Provide(ushort prefetchCount = 1)
        {
            var channelMap =_channels.GetOrAdd(Thread.CurrentThread.ManagedThreadId,
                threadId =>
                {
                    var newChannel = CreateChannel(prefetchCount);

                    return new PrefetchCountToChannelMap
                    {
                        {prefetchCount, newChannel}
                    };
                });

            if (channelMap.TryGetValue(prefetchCount, out var channel))
            {
                if (channel.IsClosed)
                {
                    channel = CreateChannel(prefetchCount);
                    channelMap[prefetchCount] = channel;
                }
            }
            else
            {
                channel = CreateChannel(prefetchCount);
                channelMap.Add(prefetchCount, channel);
            }

            return channel;
        }

        IModel CreateChannel(ushort prefetchCount)
        {
            var ch = _connectionProvider.Provide().CreateModel();
            ch.BasicQos(0, prefetchCount, false);

            return ch;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var prefetchToChannelMap in _channels.Values)
            {
                foreach (var channel in prefetchToChannelMap.Values)
                {
                    channel?.Dispose();
                }

                prefetchToChannelMap.Clear();
            }

            _channels.Clear();
        }

        class PrefetchCountToChannelMap : Dictionary<ushort, IModel>
        {

        }

        class ThreadToChannelsMap : ConcurrentDictionary<int, PrefetchCountToChannelMap>
        {

        }
    }
}