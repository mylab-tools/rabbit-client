using System;
using System.Reflection;

namespace MyLab.MqApp
{
    /// <summary>
    /// Contains MQ model description
    /// </summary>
    public class MqMsgModelDesc
    {
        /// <summary>
        /// MQ queue name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets info from specified model
        /// </summary>
        public static MqMsgModelDesc GetFromModel(object model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var modelType = model.GetType();
            var qa = modelType.GetCustomAttribute<QueueAttribute>();

            if (qa == null) throw new InvalidOperationException(
                $"Model '{modelType.FullName}' should be marked by '{typeof(QueueAttribute).FullName}' attribute");

            return new MqMsgModelDesc
            {
                QueueName = qa.QueueName
            }; 
        }
    }
}