using System;
using MyLab.StatusProvider;

namespace MyLab.Mq.StatusProvider
{
    /// <summary>
    /// Contains extension methods for <see cref="IMqStatusService"/>
    /// </summary>
    public static class MqStatusServiceExtensions
    {
        /// <summary>
        /// Report about task logic error
        /// </summary>
        public static void ConsumingError(this IMqStatusService srv, string srcQueue, Exception e)
        {
            srv.ConsumingError(srcQueue, new StatusError(e));
        }

        /// <summary>
        /// Report about message sending error
        /// </summary>
        public static void SendingError(this IMqStatusService srv, string srcQueue, Exception e)
        {
            srv.SendingError(srcQueue, new StatusError(e));
        }
    }
}