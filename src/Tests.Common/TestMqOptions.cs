using MyLab.Mq;
using MyLab.Mq.Communication;

namespace Tests.Common
{
    public static class TestMqOptions 
    {
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

        public static readonly IMqChannelProvider ChannelProvider = new MqChannelProvider(new DefaultMqConnectionProvider(Load()));
    }
}