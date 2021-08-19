using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Defines consumer logic interface
    /// </summary>
    public interface IRabbitConsumer
    {
        /// <summary>
        /// Override to implement consuming logic
        /// </summary>
        Task ConsumeAsync(BasicDeliverEventArgs deliverEventArgs);
    }
}