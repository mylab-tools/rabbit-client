using System;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Empty publishing context
    /// </summary>
    public class EmptyPubCtx : IDisposable
    {
        /// <summary>
        /// Static singleton
        /// </summary>
        public static readonly IDisposable Instance  = new EmptyPubCtx();

        EmptyPubCtx()
        {
            
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}