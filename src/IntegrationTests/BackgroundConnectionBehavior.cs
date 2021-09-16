using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Model;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class BackgroundConnectionBehavior
    {
        private readonly ITestOutputHelper _output;

        public BackgroundConnectionBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ShouldConnect()
        {
            //Arrange
            var host = new HostBuilder()
                .ConfigureServices(srv => srv
                    .AddRabbit(RabbitConnectionStrategy.Background)
                    .ConfigureRabbit(TestTools.OptionsConfigureAct)
                    .AddLogging(l => l.AddXUnit(_output))
                )
                .Build();

            var connManager = (IBackgroundRabbitConnectionManager)host.Services.GetService(typeof(IBackgroundRabbitConnectionManager));

            var ev = new ManualResetEvent(false);

            connManager.Connected += (sender, args) =>
            {
                ev.Set();
            };

            string testMsg;
            
            try
            {
                //Act
                await host.StartAsync();

                var eventOccurred = ev.WaitOne(TimeSpan.FromSeconds(3));
                if (!eventOccurred) throw new TimeoutException("Test connection timeout");

                var chProvider = (IRabbitChannelProvider)host.Services.GetService(typeof(IRabbitChannelProvider));

                var queueFactory = new RabbitQueueFactory(chProvider)
                {
                    AutoDelete = true,
                    Prefix = "test-"
                };
                
                RabbitQueue query = null;
                try
                {
                    query = queueFactory.CreateWithRandomId();

                    query.Publish("foo");

                    testMsg = query.Listen<string>().Content;

                }
                finally
                {
                    query?.Remove();
                }


            }
            finally
            {
                await host.StopAsync();
                host.Dispose();
            }

            //Assert
            Assert.Equal("foo", testMsg);
        }
    }
}