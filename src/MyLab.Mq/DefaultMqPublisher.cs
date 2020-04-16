using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Options;
using MyLab.StatusProvider;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace MyLab.Mq
{
    class DefaultMqPublisher : IMqPublisher, IDisposable
    {
        private readonly IAppStatusService _statusService;
        private readonly MqConnectionProvider _connectionProvider;
        private readonly MqChannelProvider _channelProvider;

        public DefaultMqPublisher(IOptions<MqOptions> options)
            : this(options.Value, null)
        {
        }
        public DefaultMqPublisher(IOptions<MqOptions> options, IAppStatusService statusService)
            :this(options.Value, statusService)
        {
        }

        public DefaultMqPublisher(MqOptions options, IAppStatusService statusService)
            : this(OptionToConnectionFactory(options), statusService)
        {
        }

        public DefaultMqPublisher(ConnectionFactory connectionFactory, IAppStatusService statusService)
        {
            _statusService = statusService;

            _connectionProvider = new MqConnectionProvider(connectionFactory);
            _channelProvider = new MqChannelProvider(_connectionProvider);
        }

        static ConnectionFactory OptionToConnectionFactory(MqOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.User,
                Password = options.Password
            };
        }

        public void Publish<T>(OutgoingMqEnvelop<T> envelop) where T : class
        {
            if (envelop == null) throw new ArgumentNullException(nameof(envelop));

            var msgTypeDesc = MqMsgModelDesc.GetFromModel(typeof(T));

            var resExchange = envelop.PublishTarget?.Exchange ?? 
                              msgTypeDesc?.Exchange ?? 
                              string.Empty;
            var resRouting = envelop.PublishTarget?.Routing ?? 
                             msgTypeDesc?.Routing ??
                             string.Empty;

            if(string.IsNullOrEmpty(resRouting) && string.IsNullOrEmpty(resExchange))
                throw new InvalidOperationException($"Publishing target not defined. Payload type '{typeof(T).FullName}'");

            var channel = _channelProvider.Provide();
            
            var basicProperties = CreateBasicProperties<T>(envelop, channel);

            var payloadStr = JsonConvert.SerializeObject(envelop.Message.Payload);
            var payloadBin = Encoding.UTF8.GetBytes(payloadStr);

            channel.BasicPublish(
                resExchange ?? string.Empty,
                resRouting ?? string.Empty,
                basicProperties,
                payloadBin
                );
        }

        private IBasicProperties CreateBasicProperties<T>(OutgoingMqEnvelop<T> envelop, IModel channel) where T : class
        {
            var appId = _statusService?.GetStatus().Name ?? "[undefined]";
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
                basicProperties.ReplyToAddress = msg.ReplyTo.ToPubAddr();

            if (envelop.Expiration != TimeSpan.Zero)
                basicProperties.Expiration = envelop.Expiration.TotalMilliseconds.ToString("F0");

            if (msg.Headers != null)
                basicProperties.Headers = msg.Headers.ToDictionary(h => h.Name, h => (object)h.Value);

            return basicProperties;
        }

        public void Dispose()
        {
            _channelProvider?.Dispose();
            _connectionProvider?.Dispose();
        }
    }
}