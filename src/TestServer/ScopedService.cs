using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.Mq;
using MyLab.Mq.PubSub;

namespace TestServer
{
    public class ScopedService
    {
        private readonly IMqMessageAccessor _messageAccessor;

        public ScopedService(IMqMessageAccessor messageAccessor)
        {
            _messageAccessor = messageAccessor;
        }

        public MqMessage<T> Get<T>()
        {
            return _messageAccessor.GetScopedMqMessage<T>();
        }
    }
}
