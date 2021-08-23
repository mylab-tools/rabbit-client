using System;
using System.Linq;
using Microsoft.Extensions.Options;
using MyLab.Log;
using MyLab.RabbitClient.Connection;

namespace MyLab.RabbitClient.Publishing
{
    class RabbitPublisher : IRabbitPublisher
    {
        private readonly IRabbitChannelProvider _channelProvider;
        private readonly RabbitOptions _options;

        public RabbitPublisher(
            IOptions<RabbitOptions> options,
            IRabbitChannelProvider channelProvider)
        {
            _channelProvider = channelProvider;
            _options = options.Value;
        }

        public RabbitPublisherBuilder IntoDefault(string routingKey = null)
        {
            if(_options.DefaultPub == null)
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
            return new RabbitPublisherBuilder(_channelProvider, exchange, routingKey);
        }

        public RabbitPublisherBuilder Into(string configId, string routingKey = null)
        {
            var pubCfg = _options.Pub?.FirstOrDefault(p => p.Id == configId);
            if (pubCfg == null)
                throw new InvalidOperationException("Publish config for message model not found")
                    .AndFactIs("config-id", configId);

            return new RabbitPublisherBuilder(_channelProvider, pubCfg.Exchange, routingKey ?? pubCfg.RoutingKey);
        }

        public RabbitPublisherBuilder Into<TMsg>(string routingKey = null)
        {
            var confIdAttr = (RabbitConfigIdAttribute)Attribute.GetCustomAttribute(typeof(TMsg), typeof(RabbitConfigIdAttribute));
            if(confIdAttr == null)
                throw new InvalidOperationException("Message model should be marked by " + nameof(RabbitConfigIdAttribute))
                    .AndFactIs("msg-model", typeof(TMsg).FullName);

            return Into(confIdAttr.ConfigId);
        }
    }
}