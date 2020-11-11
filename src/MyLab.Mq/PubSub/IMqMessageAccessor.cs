using System;
using RabbitMQ.Client;

namespace MyLab.Mq.PubSub
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

        void SetScopedMessage(object binBody, IBasicProperties basicProperties);
    }

    class DefaultMqMessageAccessor : IMqMessageAccessor, IMqMessageAccessorCore
    {
        private ReadOnlyMemory<byte> _binBody;
        private IBasicProperties _basicProperties;
        private bool _inited;
        private object _objBody;

        public MqMessage<T> GetScopedMqMessage<T>()
        {
            if (!_inited) return null;

            if(_objBody is T castPayload)
                return new MqMessage<T>(castPayload);
            
            if(!_binBody.IsEmpty)
                return MqMessageCreator.Create<T>(_binBody, _basicProperties);

            return null;
        }

        public void SetScopedMessage(ReadOnlyMemory<byte> binBody, IBasicProperties basicProperties)
        {
            _binBody = binBody;
            _basicProperties = basicProperties;
            _inited = true;
        }

        public void SetScopedMessage(object objectBody, IBasicProperties basicProperties)
        {
            _objBody = objectBody;
            _basicProperties = basicProperties;
            _inited = true;
        }
    }
}
