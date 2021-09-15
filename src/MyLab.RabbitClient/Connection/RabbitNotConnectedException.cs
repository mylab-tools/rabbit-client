using System;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Occurred when Rabbit connection is not established
    /// </summary>
    public class RabbitNotConnectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RabbitNotConnectedException"/>
        /// </summary>
        public RabbitNotConnectedException() : base("The Rabbit connection is not established")
        {

        }
    }
}