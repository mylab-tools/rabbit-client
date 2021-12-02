using System;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    class RabbitChannelExceptionReceiver
    {
        private readonly IDslLogger _log;
        
        public RabbitChannelExceptionReceiver(ILogger log)
        {
            _log = log.Dsl();
        }

        public IDisposable StartListen(IModel channel)
        {
            channel.CallbackException += ProcessException;

            return new Disposer(channel, ProcessException);
        }

        private void ProcessException(object sender, CallbackExceptionEventArgs e)
        {
            _log?.Error(e.Exception)
                .AndFactIs("details", e.Detail)
                .Write();
        }

        class Disposer : IDisposable
        {
            private readonly IModel _channel;
            private readonly EventHandler<CallbackExceptionEventArgs> _handler;

            public Disposer(IModel channel, EventHandler<CallbackExceptionEventArgs> handler)
            {
                _channel = channel;
                _handler = handler;
            }

            public void Dispose()
            {
                _channel.CallbackException -= _handler;
            }
        }
    }
}