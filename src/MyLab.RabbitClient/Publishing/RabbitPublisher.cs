using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Connection;

namespace MyLab.RabbitClient.Publishing
{
    class DefaultRabbitPublisher : RabbitPublisher
    {
        public DefaultRabbitPublisher(
            IOptions<RabbitOptions> options,
            IRabbitChannelProvider channelProvider,
            IEnumerable<IPublishingContext> publishingContexts,
            ILogger<DefaultRabbitPublisher> logger= null)
        : base(
            new ChannelBasedPublisherBuilderStrategy(channelProvider),
            options,
            publishingContexts,
            logger.Dsl())
        {
        }
    }

    abstract class RabbitPublisher : IRabbitPublisher
    {
        private readonly IEnumerable<IPublishingContext> _pubContexts;
        private readonly RabbitOptions _options;
        private readonly IDslLogger _log;
        private readonly IPublisherBuilderStrategy _publishBuilderStrategy;

        protected RabbitPublisher(
            IPublisherBuilderStrategy publisherBuilderStrategy,
            IOptions<RabbitOptions> options,
            IEnumerable<IPublishingContext> publishingContexts = null,
            IDslLogger logger = null)
        {
            _publishBuilderStrategy = publisherBuilderStrategy;
            _pubContexts = publishingContexts;
            _options = options.Value;
            _log = logger;
        }

        public RabbitPublisherBuilder IntoDefault(string routingKey = null)
        {
            if (_options.DefaultPub == null)
                throw new InvalidOperationException("Default publish options not found");

            return IntoExchange(
                _options.DefaultPub.Exchange,
                _options.DefaultPub.RoutingKey);
        }

        public RabbitPublisherBuilder IntoQueue(string queue)
        {
            return IntoExchange(null, queue);
        }

        public RabbitPublisherBuilder IntoExchange(string exchange, string routingKey = null)
        {
            return new RabbitPublisherBuilder(_publishBuilderStrategy, exchange, routingKey, _pubContexts)
            {
                Log = _log
            };
        }

        public RabbitPublisherBuilder Into(string configId, string routingKey = null)
        {
            var pubCfg = _options.Pub?.FirstOrDefault(p => p.Id == configId);
            if (pubCfg == null)
                throw new InvalidOperationException("Publish config for message model not found")
                    .AndFactIs("config-id", configId);

            return new RabbitPublisherBuilder(_publishBuilderStrategy, pubCfg.Exchange, routingKey ?? pubCfg.RoutingKey, _pubContexts)
            {
                Log = _log
            };
        }

        public RabbitPublisherBuilder Into<TMsg>(string routingKey = null)
        {
            var confIdAttr = (RabbitConfigIdAttribute)Attribute.GetCustomAttribute(typeof(TMsg), typeof(RabbitConfigIdAttribute));
            if (confIdAttr == null)
                throw new InvalidOperationException("Message model should be marked by " + nameof(RabbitConfigIdAttribute))
                    .AndFactIs("msg-model", typeof(TMsg).FullName);

            return Into(confIdAttr.ConfigId, routingKey);
        }
    }
}