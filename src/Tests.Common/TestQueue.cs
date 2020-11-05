using System;
using MyLab.Mq;

namespace Tests.Common
{

    public class TestQueueFactory : MqQueueFactory
    {
        public static readonly TestQueueFactory Default = new TestQueueFactory();

        public TestQueueFactory()
            :base(new DefaultMqConnectionProvider(TestMqOptions.Load()))
        {
            Prefix = "mylab:mq:test:";
            AutoDelete = true;
        }
    }

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
    }
}
