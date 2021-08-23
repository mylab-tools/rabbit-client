using System;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Specifies key for config reference
    /// </summary>
    public class RabbitConfigIdAttribute : Attribute
    {
        /// <summary>
        /// Gets configuration identifier
        /// </summary>
        public string ConfigId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitConfigIdAttribute"/>
        /// </summary>
        public RabbitConfigIdAttribute(string configId)
        {
            ConfigId = configId ?? throw new ArgumentNullException(nameof(configId));
        }
    }
}
