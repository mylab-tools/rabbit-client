using System;

namespace MyLab.Mq.Test
{
    /// <summary>
    /// Contains fake queue message processing result
    /// </summary>
    public class FakeMessageQueueProcResult
    {
        /// <summary>
        /// Is there was acknowledge
        /// </summary>
        public bool Acked { get; set; }

        /// <summary>
        /// Is there was rejected
        /// </summary>
        public bool Rejected { get; set; }

        /// <summary>
        /// Exception which is reason of rejection
        /// </summary>
        public Exception RejectionException { get; set; }

        /// <summary>
        /// Requeue flag value
        /// </summary>
        public bool RequeueFlag { get; set; }
    }
}