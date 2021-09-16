using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Manages background connection
    /// </summary>
    public interface IBackgroundRabbitConnectionManager
    {
        /// <summary>
        /// Occurred when Rabbit connected
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        /// Provides established connection
        /// </summary>
        IConnection ProvideConnection();

        /// <summary>
        /// Initiate connection
        /// </summary>
        Task ConnectAsync();
    }
}
