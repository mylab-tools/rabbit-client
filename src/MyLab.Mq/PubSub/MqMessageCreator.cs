using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MyLab.Mq.PubSub
{
    static class MqMessageCreator
    {
        public static MqMessage<T> Create<T>(ReadOnlyMemory<byte> binBody, IBasicProperties basicProperties)
        {
            var bodyStr = Encoding.UTF8.GetString(binBody.ToArray());
            var payload = JsonConvert.DeserializeObject<T>(bodyStr);

            var props = basicProperties;
            var msg = new MqMessage<T>(payload)
            {
                ReplyTo = props.ReplyTo
            };

            if (Guid.TryParse(props.CorrelationId, out var correlationId))
                msg.CorrelationId = correlationId;
            if (Guid.TryParse(props.MessageId, out var messageId))
                msg.MessageId = messageId;
            if (props.Headers != null)
            {
                msg.Headers = props.Headers
                    .Select(h => new MqHeader
                    {
                        Name = h.Key,
                        Value = h.Value.ToString()
                    })
                    .ToArray();
            }

            return msg;
        }
    }
}