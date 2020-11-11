using System;
using System.Linq;
using System.Text;
using MyLab.Mq.Communication;
using MyLab.Mq.StatusProvider;
using MyLab.StatusProvider;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MyLab.Mq.PubSub
{
    class DefaultMqPublisher : IMqPublisher
    {
        private readonly IAppStatusService _appStatusService;
        private readonly IMqStatusService _statusService;
        private readonly MqChannelProvider _channelProvider;
        
        public DefaultMqPublisher(
            IMqConnectionProvider connectionProvider, 
            IMqStatusService statusService,
            IAppStatusService appStatusService = null)
        {
            _statusService = statusService;
            _appStatusService = appStatusService;

            _channelProvider = new MqChannelProvider(connectionProvider);
        }

        public void Publish<T>(OutgoingMqEnvelop<T> envelop) where T : class
        {
            var pubTarget = GetPubTarget(envelop);
            var pubTargetName = pubTarget.Exchange ?? pubTarget.Routing;

            _statusService?.MessageStartSending(pubTargetName);

            try
            {
                PublishCore(envelop, pubTarget);
            }
            catch (Exception e)
            {
                _statusService?.SendingError(pubTargetName, e);
                throw;
            }

            _statusService?.MessageSent(pubTargetName);
        }

        void PublishCore<T>(OutgoingMqEnvelop<T> envelop, PublishTarget pubTarget) where T : class
        {
            if (envelop == null) throw new ArgumentNullException(nameof(envelop));
            
            if (string.IsNullOrEmpty(pubTarget.Routing) && string.IsNullOrEmpty(pubTarget.Exchange))
                throw new InvalidOperationException($"Publishing target not defined. Payload type '{typeof(T).FullName}'");

            var channel = _channelProvider.Provide();

            var basicProperties = CreateBasicProperties<T>(envelop, channel);

            var payloadStr = JsonConvert.SerializeObject(envelop.Message.Payload);
            var payloadBin = Encoding.UTF8.GetBytes(payloadStr);

            channel.BasicPublish(
                pubTarget.Exchange,
                pubTarget.Routing,
                basicProperties,
                payloadBin
            );
        }

        PublishTarget GetPubTarget<T>(OutgoingMqEnvelop<T> envelop)
        {
            var msgTypeDesc = MqMsgModelDesc.GetFromModel(typeof(T));

            var resExchange = envelop.PublishTarget?.Exchange ??
                              msgTypeDesc?.Exchange ??
                              string.Empty;
            var resRouting = envelop.PublishTarget?.Routing ??
                             msgTypeDesc?.Routing ??
                             string.Empty;
            return new PublishTarget
            {
                Routing = resRouting,
                Exchange = resExchange
            };
        }

        private IBasicProperties CreateBasicProperties<T>(OutgoingMqEnvelop<T> envelop, IModel channel) where T : class
        {
            var appId = _appStatusService?.GetStatus().Name ?? "[undefined]";
            var msg = envelop.Message;
            var unixNow = Convert.ToInt64((DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);

            var basicProperties = channel.CreateBasicProperties();
            basicProperties.Timestamp = new AmqpTimestamp(unixNow);
            basicProperties.Type = typeof(T).FullName;
            basicProperties.ContentType = "application/json";
            basicProperties.AppId = appId;
            basicProperties.CorrelationId = msg.CorrelationId.ToString("N");
            basicProperties.MessageId = msg.MessageId.ToString("N");

            if (msg.ReplyTo != null)
                basicProperties.ReplyTo = msg.ReplyTo;

            if (envelop.Expiration != TimeSpan.Zero)
                basicProperties.Expiration = envelop.Expiration.TotalMilliseconds.ToString("F0");

            if (msg.Headers != null)
                basicProperties.Headers = msg.Headers.ToDictionary(h => h.Name, h => (object)h.Value);

            return basicProperties;
        }
    }
}