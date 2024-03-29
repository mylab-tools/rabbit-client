﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.Log;
using MyLab.Log.Dsl;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Manages and provides Rabbit channels
    /// </summary>
    public class RabbitChannelProvider : IRabbitChannelProvider
    {
        private readonly IRabbitConnectionProvider _connectionProvider;
        private const int MaxChannelCounter = 100;

        private readonly object _lock = new object();

        readonly Queue<ChannelUnit> _freeChannels = new Queue<ChannelUnit>();
        readonly List<ChannelUnit> _usedChannels = new List<ChannelUnit>();
        private readonly IDslLogger _log;

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitChannelProvider"/>
        /// </summary>
        public RabbitChannelProvider(IRabbitConnectionProvider connectionProvider, ILogger<RabbitChannelProvider> logger = null)
        {
            _connectionProvider = connectionProvider;
            connectionProvider.Reconnected += Reconnected;

            _log = logger?.Dsl();
        }

        /// <inheritdoc />
        public RabbitChannelUsing Provide()
        {
            var ch = TakeChannel();
            return new RabbitChannelUsing(ch.Channel, new Liberator(ch, FreeChannel));
        }

        /// <inheritdoc />
        public void Use(Action<IModel> act)
        {
            var ch = TakeChannel();
            try
            {
                act(ch.Channel);
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
                await act(ch.Channel);
            }
            finally
            {
                FreeChannel(ch);
            }
        }

        ChannelUnit TakeChannel()
        {
            lock (_lock)
            {
                if (!_freeChannels.TryDequeue(out var channel))
                {
                    IModel newChannel;
                    try
                    {
                        var conn = _connectionProvider.Provide();

                        if (conn == null)
                            throw new InvalidOperationException("The Connection Provider returns the 'null' instead a connection");

                        newChannel = conn.CreateModel();

                        if (newChannel == null)
                            throw new InvalidOperationException("The Connection creates the 'null' instead a new channel");

                        _log?.Action("New Rabbit channel has created")
                            .AndFactIs("channel-number", newChannel?.ChannelNumber)
                            .Write();
                    }
                    catch (ChannelAllocationException e)
                    {
                        e.AndFactIs("concerned-chanel", e.Channel)
                         .AndFactIs("used-channel-list-count", _usedChannels.Count);

                        throw;
                    }

                    newChannel.BasicQos(0, 1, false);

                    channel = new ChannelUnit(newChannel);
                }

                _usedChannels.Add(channel);

                return channel;
            }
        }

        void FreeChannel(ChannelUnit channel)
        {
            lock (_lock)
            {
                _usedChannels.Remove(channel);

                if (channel.Counter >= MaxChannelCounter)
                {
                    channel.Channel.Close();
                    channel.Channel.Dispose();
                }
                else
                {
                    _freeChannels.Enqueue(channel);
                    channel.Counter += 1;
                }
            }
        }

        private void Reconnected(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _freeChannels.Clear();
                _usedChannels.Clear();
            }
        }

        class Liberator : IDisposable
        {
            private readonly ChannelUnit _channel;
            private readonly Action<ChannelUnit> _liberateAct;

            public Liberator(ChannelUnit channel, Action<ChannelUnit> liberateAct)
            {
                _channel = channel;
                _liberateAct = liberateAct;
            }

            public void Dispose()
            {
                _liberateAct(_channel);
            }
        }

        class ChannelUnit
        {
            public IModel Channel { get; }
            public int Counter { get; set; } = 0;

            public ChannelUnit(IModel channel)
            {
                Channel = channel;
            }
        }
    }
}