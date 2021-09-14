using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Publishing;

namespace LoadTest.Publisher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
                throw new InvalidOperationException("Queue not specified");
            var queueName = args[0];

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(srv => srv
                    .ConfigureRabbit(opt =>
                    {
                        opt.Host = "localhost";
                        opt.Port = 5672;
                        opt.User = "guest";
                        opt.Password = "guest";
                        opt.DefaultPub = new PublishOptions
                        {
                            RoutingKey = queueName
                        };
                    })
                    .AddLogging(l => l.AddConsole().AddFilter(l => true))
                    .AddHostedService<TestService>())
                .Build();

            await host.StartAsync();
            
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();

            await host.StopAsync();
        }
    }
}
