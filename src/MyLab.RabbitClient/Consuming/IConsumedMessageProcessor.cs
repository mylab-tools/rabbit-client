using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Processed consumed message
    /// </summary>
    public interface IConsumedMessageProcessor
    {
        /// <summary>
        /// Processes consumed message
        /// </summary>
        public void Process(BasicDeliverEventArgs deliverEventArgs);
    }
}
