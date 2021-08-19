namespace MyLab.RabbitClient
{
    /// <summary>
    /// Contains connection options
    /// </summary>
    public class RabbitOptions
    {
        /// <summary>
        /// Server host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Virtual host
        /// </summary>
        public string VHost { get; set; }

        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; set; } = 5672;
        /// <summary>
        /// Login user
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Login password
        /// </summary>
        public string Password { get; set; }
    }
}
