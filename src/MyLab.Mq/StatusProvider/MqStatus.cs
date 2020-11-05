using System;
using System.Collections.Generic;
using MyLab.StatusProvider;

namespace MyLab.Mq.StatusProvider
{
    /// <summary>
    /// Contains MQ specific status
    /// </summary>
    public class MqStatus : ICloneable
    {
        /// <summary>
        /// Contains consuming status for queues
        /// </summary>
        public IDictionary<string, ConsumeMqStatus> Consume { get; }
        /// <summary>
        /// Contains publishing status for exchanges or queues
        /// </summary>
        public IDictionary<string, PublishMqStatus> Publish { get; }

        public MqStatus()
        {
            Consume = new Dictionary<string, ConsumeMqStatus>();   
            Publish = new Dictionary<string, PublishMqStatus>();   
        }

        public MqStatus(MqStatus origin)
        {
            Consume = new Dictionary<string, ConsumeMqStatus>(origin.Consume);
            Publish = new Dictionary<string, PublishMqStatus>(origin.Publish);
        }

        public object Clone()
        {
            return new MqStatus(this);
        }
    }

    public class ConsumeMqStatus
    {
        /// <summary>
        /// Gets date-time when last incoming message has received
        /// </summary>
        public DateTime? LastTime { get; set; }
        /// <summary>
        /// An error which occured when last incoming message processing
        /// </summary>
        public StatusError LastError { get; set; }
    }

    public class PublishMqStatus
    {
        /// <summary>
        /// Gets date-time when last outgoing message has sent
        /// </summary>
        public DateTime? LastTime { get; set; }
        /// <summary>
        /// An error which occured when last outgoing message sending
        /// </summary>
        public StatusError LastError { get; set; }
    }
}