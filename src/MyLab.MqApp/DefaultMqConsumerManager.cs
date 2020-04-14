using System.Collections.Generic;

namespace MyLab.MqApp
{
    class DefaultMqConsumerManager : IMqConsumerManager
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConsumerManager"/>
        /// </summary>
        public DefaultMqConsumerManager(ConsumerMap consumerMap)
            :this(consumerMap.ToDictionary())
        {
            
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConsumerManager"/>
        /// </summary>
        public DefaultMqConsumerManager(IDictionary<string, IMqConsumer> consumerMap)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConsumerManager"/>
        /// </summary>
        public DefaultMqConsumerManager()
            :this(new Dictionary<string, IMqConsumer>())
        {
            
        }
    }
}