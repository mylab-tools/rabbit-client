using MyLab.StatusProvider;

namespace MyLab.Mq.StatusProvider
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
}
