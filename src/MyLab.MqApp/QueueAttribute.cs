using System;

namespace MyLab.MqApp
{
    /// <summary>
    /// Specifies MQ message model
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class QueueAttribute : Attribute
    {
        /// <summary>
        /// Gets target queue name
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="QueueAttribute"/>
        /// </summary>
        public QueueAttribute(string queueName)
        {
            QueueName = queueName;
        }
    }
}
