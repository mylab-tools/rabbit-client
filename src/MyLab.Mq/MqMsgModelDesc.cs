using System;
using System.Reflection;

namespace MyLab.Mq
{
    /// <summary>
    /// Contains MQ model description
    /// </summary>
    public class MqMsgModelDesc
    {
        /// <summary>
        /// MQ queue name or routing key
        /// </summary>
        public string Routing { get; set; }

        /// <summary>
        /// MQ exchange name
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Gets info from specified model
        /// </summary>
        public static MqMsgModelDesc GetFromModel(Type modelType)
        {
            var qa = modelType.GetCustomAttribute<MqAttribute>();
            if (qa == null) return null;

            return new MqMsgModelDesc
            {
                Routing = qa.Routing,
                Exchange = qa.Exchange,
            }; 
        }
    }
}