using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq.PubSub
{
    class ChannelMessageReceivingController
    {
        private readonly QueueMessageProcessor _messageProcessor;
        private readonly Dictionary<string, QueueConsumerDesc> _queueToConsumerDescMap = new Dictionary<string, QueueConsumerDesc>();

        public ChannelMessageReceivingController(QueueMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        }

        public void Register(IModel channel, string queueName)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            if (_queueToConsumerDescMap.ContainsKey(queueName))
                throw new InvalidOperationException($"Queue '{queueName}' already registered");

            var systemConsumer = new AsyncEventingBasicConsumer(channel);

            systemConsumer.Received += _messageProcessor.ConsumerReceivedAsync;

            channel.BasicConsume(
                queue: queueName,
                consumerTag: queueName,
                consumer: systemConsumer);

            _queueToConsumerDescMap.Add(queueName, new QueueConsumerDesc
            {
                Channel = channel,
                SystemConsumer = systemConsumer
            });
        }

        public void Unregister(string queueName)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            if (!_queueToConsumerDescMap.TryGetValue(queueName, out var consumerDesc))
                return;

            consumerDesc.SystemConsumer.Received -= _messageProcessor.ConsumerReceivedAsync;
            consumerDesc.Channel.BasicCancelNoWait(queueName);

            _queueToConsumerDescMap.Remove(queueName);
        }

        public void Clear()
        {
            var regedChannels = _queueToConsumerDescMap.Keys.ToArray();

            foreach (var regedChannel in regedChannels)
            {
                Unregister(regedChannel);
            }
        }

        class QueueConsumerDesc
        {
            public IModel Channel { get; set; }
            public AsyncEventingBasicConsumer SystemConsumer { get; set; }
        }
    }
}