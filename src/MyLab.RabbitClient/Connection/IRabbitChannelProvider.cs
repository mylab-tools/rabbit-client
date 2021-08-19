using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Provides channels
    /// </summary>
    public interface IRabbitChannelProvider
    {
        /// <summary>
        /// Provide channel using
        /// </summary>
        RabbitChannelUsing Provide();

        /// <summary>
        /// Safe channel using with auto-free
        /// </summary>
        void Use(Action<IModel> act);

        /// <summary>
        /// Safe async channel using with auto-free
        /// </summary>
        Task UseAsync(Func<IModel, Task> act);
    }
}
