using System;

namespace MyLab.Mq
{
    /// <summary>
    /// Specifies MQ publishing parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MqAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets exchange name 
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Gets or sets routing key or queue name
        /// </summary>
        public string Routing { get; set; }
    }
}
