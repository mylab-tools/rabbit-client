using System;
using MyLab.StatusProvider;

namespace MyLab.Mq
{
    /// <summary>
    /// Provides abilities to modify MQ status
    /// </summary>
    public interface IMqStatusService
    {
        /// <summary>
        /// Report about connection to queue for listening
        /// </summary>
        void QueueConnected(string queueName);
        /// <summary>
        /// Report about incoming message has received 
        /// </summary>
        void MessageReceived(string srcQueue);
        /// <summary>
        /// Report about incoming message succeed processing
        /// </summary>
        void MessageProcessed(string srcQueue);
        /// <summary>
        /// Report about error when incoming message processing
        /// </summary>
        void ConsumingError(string srcQueue, StatusError e);
        /// <summary>
        /// Report about outgoing message sending started
        /// </summary>
        void MessageStartSending(string pubTargetName);
        /// <summary>
        /// Report about outgoing message has sent
        /// </summary>
        void MessageSent(string pubTargetName);
        /// <summary>
        /// Report about message sending error
        /// </summary>
        void SendingError(string pubTargetName, StatusError e);
    }

    /// <summary>
    /// Contains extension methods for <see cref="IMqStatusService"/>
    /// </summary>
    public static class MqStatusServiceExtensions
    {
        /// <summary>
        /// Report about task logic error
        /// </summary>
        public static void ConsumingError(this IMqStatusService srv, string srcQueue, Exception e)
        {
            srv.ConsumingError(srcQueue, new StatusError(e));
        }

        /// <summary>
        /// Report about message sending error
        /// </summary>
        public static void SendingError(this IMqStatusService srv, string srcQueue, Exception e)
        {
            srv.SendingError(srcQueue, new StatusError(e));
        }
    }

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
