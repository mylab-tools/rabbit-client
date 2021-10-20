using System;

namespace MyLab.RabbitClient
{
    /// <summary>
    /// Empty publishing context
    /// </summary>
    public class EmptyCtx : IDisposable
    {
        /// <summary>
        /// Static singleton
        /// </summary>
        public static readonly IDisposable Instance  = new EmptyCtx();

        EmptyCtx()
        {
            
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}