namespace MyLab.Mq
{
    /// <summary>
    /// Defines a Message Queue publisher
    /// </summary>
    public interface IMqPublisher 
    {
        /// <summary>
        /// Publish message into queue
        /// </summary>
        void Publish<T>(OutgoingMqEnvelop<T> envelop) where T : class;
    }
}