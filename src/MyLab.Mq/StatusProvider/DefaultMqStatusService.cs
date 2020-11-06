using System;
using MyLab.StatusProvider;

namespace MyLab.Mq.StatusProvider
{
    class DefaultMqStatusService : IMqStatusService
    {
        private readonly Lazy<MqStatus> _status;

        public DefaultMqStatusService(IServiceProvider serviceProvider)
        {
            _status = new Lazy<MqStatus>(() =>
            {
                var statusService = (IAppStatusService)serviceProvider.GetService(typeof(IAppStatusService));
                return statusService != null
                    ? statusService.RegSubStatus<MqStatus>()
                    : new MqStatus();
            });
        }

        public void QueueConnected(string queueName)
        {
            _status.Value.Consume.Add(queueName, new ConsumeMqStatus());
        }

        public void QueueDisconnected(string queueName)
        {
            if(_status.Value.Consume.ContainsKey(queueName))
                _status.Value.Consume.Remove(queueName);
        }

        public void MessageReceived(string srcQueue)
        {
            RetrieveConsumeStatus(srcQueue).LastTime = DateTime.Now;
        }

        public void MessageProcessed(string srcQueue)
        {
            RetrieveConsumeStatus(srcQueue).LastError = null;
        }

        public void ConsumingError(string srcQueue, StatusError e)
        {
            RetrieveConsumeStatus(srcQueue).LastError = e;
        }

        public void MessageStartSending(string pubTargetName)
        {
            RetrievePubStatus(pubTargetName).LastTime = DateTime.Now;
        }

        public void MessageSent(string pubTargetName)
        {
            RetrievePubStatus(pubTargetName).LastError = null;
        }

        public void SendingError(string pubTargetName, StatusError e)
        {
            RetrievePubStatus(pubTargetName).LastError = e;
        }

        PublishMqStatus RetrievePubStatus(string pubTarget)
        {
            if (!_status.Value.Publish.TryGetValue(pubTarget, out var stat))
            {
                stat = new PublishMqStatus();
                _status.Value.Publish.Add(pubTarget, stat);
            }

            return stat;
        }

        ConsumeMqStatus RetrieveConsumeStatus(string pubTarget)
        {
            if (!_status.Value.Consume.TryGetValue(pubTarget, out var stat))
            {
                stat = new ConsumeMqStatus();
                _status.Value.Consume.Add(pubTarget, stat);
            }

            return stat;
        }
    }
}