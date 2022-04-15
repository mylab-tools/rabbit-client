using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Consuming;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Publishing
{
    class EmulatorRabbitPublisher : RabbitPublisher
    {
        public EmulatorRabbitPublisher(
            IConsumingLogicStrategy consumingLogicStrategy,
            IServiceProvider serviceProvider,
            IOptions<ConsumerRegistrarSource> consumerRegistrarSource,
            IOptions<RabbitOptions> options,
            IEnumerable<IPublishingContext> publishingContexts = null,
            ILogger<EmulatorRabbitPublisher> log = null)
        :base(CreateStrategy(consumingLogicStrategy, consumerRegistrarSource, serviceProvider, log), options, publishingContexts,log?.Dsl())
        {
        }

        static IPublisherBuilderStrategy CreateStrategy(
            IConsumingLogicStrategy consumingLogicStrategy,
            IOptions<ConsumerRegistrarSource> consumerRegistrarSource,
            IServiceProvider serviceProvider,
            ILogger log)
        {
            var consumerRegistry = new ConsumerRegistry();

            consumerRegistry.RegisterConsumersFromSource(consumerRegistrarSource.Value, serviceProvider);

            var consumingLogic = new ConsumingLogic(consumerRegistry, serviceProvider)
            {
                Log = log?.Dsl()
            };

            return new EmulatorPublisherBuilderStrategy(consumingLogic, consumingLogicStrategy);
        }
    }

    class EmulatorPublisherBuilderStrategy : IPublisherBuilderStrategy
    {
        private readonly ConsumingLogic _consumingLogic;
        private readonly IConsumingLogicStrategy _consumingLogicStrategy;

        public EmulatorPublisherBuilderStrategy(ConsumingLogic consumingLogic, IConsumingLogicStrategy consumingLogicStrategy)
        {
            _consumingLogic = consumingLogic;
            _consumingLogicStrategy = consumingLogicStrategy;
        }

        public void Use(Action<IPublisherBuilderStrategyUsing> use)
        {
            var strategyUsing = new EmulatorPublisherBuilderStrategyUsing(_consumingLogic, _consumingLogicStrategy);

            use(strategyUsing);
        }
    }

    class EmulatorPublisherBuilderStrategyUsing : IPublisherBuilderStrategyUsing
    {
        private readonly ConsumingLogic _consumingLogic;
        private readonly IConsumingLogicStrategy _consumingLogicStrategy;

        public EmulatorPublisherBuilderStrategyUsing(ConsumingLogic consumingLogic, IConsumingLogicStrategy consumingLogicStrategy)
        {
            _consumingLogic = consumingLogic;
            _consumingLogicStrategy = consumingLogicStrategy;
        }

        public IBasicProperties CreateBasicProperties()
        {
            return new EmulatorBasicProperties();
        }

        public void Publish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] content)
        {
            var eventArgs = new BasicDeliverEventArgs(routingKey, 0, false, exchange, routingKey, basicProperties, content);

            _consumingLogic.ConsumeMessageAsync(eventArgs, _consumingLogicStrategy).Wait();
        }
    }

    class DefaultEmulatorConsumingLogicStrategy : IConsumingLogicStrategy
    {
        public void Ack(ulong deliveryTag)
        {
        }

        public void Nack(ulong deliveryTag)
        {
        }
    }

    class EmulatorBasicProperties : IBasicProperties
    {
        public ushort ProtocolClassId { get; set; }
        public string ProtocolClassName { get; set; }
        public string AppId { get; set; }
        public string ClusterId { get; set; }
        public string ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public string CorrelationId { get; set; }
        public byte DeliveryMode { get; set; }
        public string Expiration { get; set; }
        public IDictionary<string, object> Headers { get; set; }
        public string MessageId { get; set; }
        public bool Persistent { get; set; }
        public byte Priority { get; set; }
        public string ReplyTo { get; set; }
        public PublicationAddress ReplyToAddress { get; set; }
        public AmqpTimestamp Timestamp { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }
        public void ClearAppId()
        {
            AppId = default;
        }

        public void ClearClusterId()
        {
            ClusterId = default;
        }

        public void ClearContentEncoding()
        {
            ContentEncoding = default;
        }

        public void ClearContentType()
        {
            ContentType = default;
        }

        public void ClearCorrelationId()
        {
            CorrelationId = default;
        }

        public void ClearDeliveryMode()
        {
            DeliveryMode = default;
        }

        public void ClearExpiration()
        {
            Expiration = default;
        }

        public void ClearHeaders()
        {
            Headers = default;
        }

        public void ClearMessageId()
        {
            MessageId = default;
        }

        public void ClearPriority()
        {
            Priority = default;
        }

        public void ClearReplyTo()
        {
            ReplyTo = default;
        }

        public void ClearTimestamp()
        {
            Timestamp = default;
        }

        public void ClearType()
        {
            Type = default;
        }

        public void ClearUserId()
        {
            UserId = default;
        }

        public bool IsAppIdPresent()
        {
            return AppId != default;
        }

        public bool IsClusterIdPresent()
        {
            return ClusterId != default;
        }

        public bool IsContentEncodingPresent()
        {
            return ContentEncoding != default;
        }

        public bool IsContentTypePresent()
        {
            return ContentType != default;
        }

        public bool IsCorrelationIdPresent()
        {
            return CorrelationId != default;
        }

        public bool IsDeliveryModePresent()
        {
            return DeliveryMode != default;
        }

        public bool IsExpirationPresent()
        {
            return Expiration != default;
        }

        public bool IsHeadersPresent()
        {
            return Headers != default;
        }

        public bool IsMessageIdPresent()
        {
            return MessageId != default;
        }

        public bool IsPriorityPresent()
        {
            return Priority != default;
        }

        public bool IsReplyToPresent()
        {
            return ReplyTo != default;
        }

        public bool IsTimestampPresent()
        {
            return Timestamp.UnixTime != default;
        }

        public bool IsTypePresent()
        {
            return Type != default;
        }

        public bool IsUserIdPresent()
        {
            return UserId != default;
        }
    }
}
