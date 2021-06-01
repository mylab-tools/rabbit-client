using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;

namespace MyLab.Mq.Communication
{
    class MqChannelProvider : IDisposable, IMqChannelProvider
    {
        private readonly IMqConnectionProvider _connectionProvider;
        private readonly ThreadToChannelsMap _channels = new ThreadToChannelsMap();

        public int ChannelCount => _channels.Count;

        public MqChannelProvider(IMqConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

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