using System;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Connection;
using RabbitMQ.Client;

namespace IntegrationTests
{
    static class TestTools
    {
        public static RabbitChannelProvider ChannelProvider { get; }

        public static RabbitOptions Options { get; }
        public static Action<RabbitOptions> OptionsConfigureAct { get; }
        static TestTools()
        {
            Options = new RabbitOptions
            {
                Host = "localhost",
                Port = 10160,
                User = "guest",
                Password = "guest"
            };

            var connProvider = new RabbitConnectionProvider(Options);
            ChannelProvider = new RabbitChannelProvider(connProvider);

            OptionsConfigureAct = opts =>
            {
                opts.Host = Options.Host;
                opts.Port = Options.Port;
                opts.User = Options.User;
                opts.Password = Options.Password;
            };
        }
    }
}