using System;
using MyLab.Mq;
using MyLab.Mq.Communication;

namespace Tests.Common
{
    public static class TestMqTools
    {
        private static DefaultMqChannelProvider _chProvider;
        private static DefaultMqConnectionProvider _connProvider;

        public static IMqConnectionProvider ConnectionProvider => _connProvider;
        public static IMqChannelProvider ChannelProvider => _chProvider;

        public static int ChannelsCount => _chProvider.ChannelCount;

        static TestMqTools()
        {
            Init();
        }

        public static MqOptions Load()
        {
            return new MqOptions
            {
                Host = "127.0.0.1",
                Password = "guest",
                User = "guest",
                Port = 10160
            };
        }

        public static void ConfigureAction(MqOptions options)
        {
            var actOptions = Load();
            options.User = actOptions.User;
            options.Password = actOptions.Password;
            options.Host = actOptions.Host;
            options.Port = actOptions.Port;
        }

        public static void Close()
        {
            ((IDisposable)ChannelProvider).Dispose();
            ConnectionProvider.Dispose();
        }

        public static void Reset()
        {
            Close();
            Init();
        }

        static void Init()
        {
            _connProvider = new DefaultMqConnectionProvider(Load());
            _chProvider = new DefaultMqChannelProvider(ConnectionProvider);
        }
    }
}