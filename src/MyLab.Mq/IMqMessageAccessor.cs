using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    /// <summary>
    /// Provides current scope MQ incoming message
    /// </summary>
    public interface IMqMessageAccessor
    {
        MqMessage<T> GetScopedMqMessage<T>();
    }

    interface IMqMessageAccessorCore
    {
        void SetScopedMessage(ReadOnlyMemory<byte> binBody, IBasicProperties basicProperties);
    }

    class DefaultMqMessageAccessor : IMqMessageAccessor, IMqMessageAccessorCore
    {
        private ReadOnlyMemory<byte> _binBody;
        private IBasicProperties _basicProperties;
        private bool _inited;

        public MqMessage<T> GetScopedMqMessage<T>()
        {
            return _inited
                ? MqMessageCreator.Create<T>(_binBody, _basicProperties)
                : null;
        }

        public void SetScopedMessage(ReadOnlyMemory<byte> binBody, IBasicProperties basicProperties)
        {
            _binBody = binBody;
            _basicProperties = basicProperties;
            _inited = true;
        }
    }
}
