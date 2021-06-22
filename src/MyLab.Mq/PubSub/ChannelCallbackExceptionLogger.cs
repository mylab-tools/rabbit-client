using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq.PubSub
{
    class ChannelCallbackExceptionLogger
    {
        private readonly IDslLogger _logger;
        private readonly Dictionary<IModel, List<string>> _channelsToQueueMap = new Dictionary<IModel, List<string>>();

        public ChannelCallbackExceptionLogger(ILogger logger)
        {
            _logger = logger.Dsl();
        }

        public void Register(IModel channel, string queue)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            if (_channelsToQueueMap.TryGetValue(channel, out var queueList))
            {
                if(!queueList.Contains(queue))
                    queueList.Add(queue);
                return;
            }

            channel.CallbackException += ProcessException;
            _channelsToQueueMap.Add(channel, new List<string>{queue});
        }

        public void Unregister(string queue)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var channels = _channelsToQueueMap
                .Where(chk => chk.Value.Contains(queue))
                .ToArray();

            foreach (var chp in channels)
            {
                chp.Value.Remove(queue);

                if (chp.Value.Count == 0)
                {
                    _channelsToQueueMap.Remove(chp.Key);
                    chp.Key.CallbackException -= ProcessException;
                }
            }
        }

        public void Clear()
        {
            var channels = _channelsToQueueMap.Keys.ToArray();

            foreach (var channel in channels)
            {
                channel.CallbackException -= ProcessException;
            }

            _channelsToQueueMap.Clear();
        }

        private void ProcessException(object? sender, CallbackExceptionEventArgs e)
        {
            _logger
                .Error(e.Exception)
                .Write();
        }
    }
}