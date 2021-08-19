using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Manages and provides Rabbit channels
    /// </summary>
    public class RabbitChannelProvider : IRabbitChannelProvider
    {
        private IConnection _connection;
        private readonly object _lock = new object();

        readonly Queue<IModel> _freeChannels = new Queue<IModel>();
        readonly List<IModel> _usedChannels = new List<IModel>();

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitChannelProvider"/>
        /// </summary>
        public RabbitChannelProvider(IRabbitConnectionProvider connectionProvider)
        {
            connectionProvider.Reconnected += Reconnected;

            _connection = connectionProvider.Provide();
        }

        /// <inheritdoc />
        public RabbitChannelUsing Provide()
        {
            var ch = TakeChannel();
            return new RabbitChannelUsing(ch, new Liberator(ch, FreeChannel));
        }

        /// <inheritdoc />
        public void Use(Action<IModel> act)
        {
            var ch = TakeChannel();
            try
            {
                act(ch);
            }
            finally
            {
                FreeChannel(ch);
            }
        }

        /// <inheritdoc />
        public async Task UseAsync(Func<IModel, Task> act)
        {
            var ch = TakeChannel();
            try
            {
                await act(ch);
            }
            finally
            {
                FreeChannel(ch);
            }
        }

        IModel TakeChannel()
        {
            lock (_lock)
            {
                if (!_freeChannels.TryDequeue(out var channel))
                {
                    channel = _connection.CreateModel();
                }

                _usedChannels.Add(channel);

                return channel;
            }
        }

        void FreeChannel(IModel channel)
        {
            lock (_lock)
            {
                _usedChannels.Remove(channel);
                _freeChannels.Enqueue(channel);
            }
        }

        private void Reconnected(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _freeChannels.Clear();
                _usedChannels.Clear();

                var cp = (IRabbitConnectionProvider)sender;
                _connection = cp.Provide();
            }
        }

        class Liberator : IDisposable
        {
            private readonly IModel _channel;
            private readonly Action<IModel> _liberateAct;

            public Liberator(IModel channel, Action<IModel> liberateAct)
            {
                _channel = channel;
                _liberateAct = liberateAct;
            }

            public void Dispose()
            {
                _liberateAct(_channel);
            }
        }
    }
}