using System;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    class MqChannelProvider : IDisposable
    {
        private readonly IMqConnectionProvider _connectionProvider;
        private readonly Dictionary<int, IModel> _channels = new Dictionary<int, IModel>();

        public MqChannelProvider(IMqConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public IModel Provide()
        {
            if (!_channels.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var channel))
            {
                channel = _connectionProvider.Provide().CreateModel();
                _channels.Add(Thread.CurrentThread.ManagedThreadId, channel);
            }
            else
            {
                if (channel.IsClosed)
                {
                    channel = _connectionProvider.Provide().CreateModel();
                    _channels[Thread.CurrentThread.ManagedThreadId]= channel;
                }
            }

            return channel;
        }

        public void Dispose()
        {
            foreach (var channel in _channels.Values)
            {
                channel?.Dispose();
            }

            _channels.Clear();
        }
    }
}