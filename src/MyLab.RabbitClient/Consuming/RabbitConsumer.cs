using System;
using System.Text;
using System.Threading.Tasks;
using MyLab.Log;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Provides deserialized consumed message to processing
    /// </summary>
    public abstract class RabbitConsumer<TContent> : IRabbitConsumer
    {
        /// <inheritdoc />
        public Task ConsumeAsync(BasicDeliverEventArgs deliverEventArgs)
        {
            var bodyStr = Encoding.UTF8.GetString(deliverEventArgs.Body.ToArray());
            TContent payload;

            if (typeof(TContent) == typeof(string))
            {
                payload = (TContent)(object)bodyStr;
            }
            else
            {
                try
                {
                    payload = JsonConvert.DeserializeObject<TContent>(bodyStr);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Message parsing error", e)
                        .AndFactIs("src", bodyStr);
                }
            }

            var msg= new ConsumedMessage<TContent>(payload, deliverEventArgs);

            return ConsumeMessageAsync(msg);
        }

        /// <summary>
        /// Override to implement consumed message processing
        /// </summary>
        protected abstract Task ConsumeMessageAsync(ConsumedMessage<TContent> consumedMessage);
    }
}