using System;
using System.Net;
using System.Threading.Tasks;
using HealthTestService;
using Microsoft.Extensions.DependencyInjection;
using MyLab.ApiClient;
using MyLab.ApiClient.Test;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Consuming;
using MyLab.RabbitClient.Model;
using RabbitMQ.Client.Events;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class HealthCheckBehavior : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly TestApi<Startup, IHealthCheckService> _api;

        public HealthCheckBehavior(ITestOutputHelper output)
        {
            _output = output;
            _api = new TestApi<Startup, IHealthCheckService>
            {
                Output = output
            };
        }

        [Fact]
        public async Task ShouldProvideUnhealthy()
        {
            //Arrange
            var srv = _api.Start(sc =>
            {
                sc.AddRabbit();
            });

            //Act
            var heathCheckResult = await srv.Call(s => s.HealthCheck());

            string msg = heathCheckResult.ResponseContent;

            //Assert
            Assert.Equal(HttpStatusCode.OK, heathCheckResult.StatusCode);
            Assert.Equal("Unhealthy", msg);
        }

        [Fact]
        public async Task ShouldProvideHealthy()
        {
            //Arrange
            var queue = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            }.CreateWithRandomId();

            var srv = _api.Start(sc =>
            {
                sc.AddRabbit(RabbitConnectionStrategy.Background);
                sc.AddRabbitConsumer<TestConsumer>(queue.Name);
                sc.ConfigureRabbit(TestTools.OptionsConfigureAct);
            });

            //Act
            var heathCheckResult = await srv.Call(s => s.HealthCheck());

            string msg = heathCheckResult.ResponseContent;

            //Assert
            Assert.Equal(HttpStatusCode.OK, heathCheckResult.StatusCode);
            Assert.Equal("Healthy", msg);
        }

        [Api]
        interface IHealthCheckService
        {
            [Get("health")]
            Task<string> HealthCheck();
        }

        public void Dispose()
        {
            _api?.Dispose();
        }
    }

    public class TestConsumer : IRabbitConsumer
    {
        public Task ConsumeAsync(BasicDeliverEventArgs deliverEventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
