using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Model;

namespace LoadTest.Listener
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var queueName = CreateQueue();

            Console.WriteLine("Queue created: " + queueName);

            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices(srv => srv
                    .AddRabbitConsumer<TestConsumer>(queueName)
                    .ConfigureRabbit(opt =>
                    {
                        opt.Host = "localhost";
                        opt.Port = 5672;
                        opt.User = "guest";
                        opt.Password = "guest";
                    })
                    .AddLogging(l => l.AddConsole()))
                .Build();

            await host.StartAsync();

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();

            await host.StopAsync();
        }

        private static string CreateQueue()
        {
            var opts = new RabbitOptions
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest"
            };

            var connProvider = new LazyRabbitConnectionProvider(opts);
            var chProvider = new RabbitChannelProvider(connProvider);

            var queueFactory = new RabbitQueueFactory(chProvider)
            {
                AutoDelete = true,
                Prefix = "loadtest-"
            };

            var queue = queueFactory.CreateWithRandomId();

            return queue.Name;
        }
    }
}
