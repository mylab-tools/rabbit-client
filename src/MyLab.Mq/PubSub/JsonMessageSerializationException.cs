using System;

namespace MyLab.Mq.PubSub
{
    class JsonMessageSerializationException : Exception
    {
        public string Content { get; }

        public JsonMessageSerializationException(string content, Exception inner)
            : base("MQ message json parsing error", inner)
        {
            Content = content;
        }
    }
}