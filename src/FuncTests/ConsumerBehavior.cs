//using System;
//using System.Threading.Tasks;
//using LoadTestServer;
//using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using MyLab.Mq;
//using MyLab.Mq.PubSub;
//using MyLab.Mq.Test;
//using Xunit;
//using Xunit.Abstractions;

//namespace FuncTests
//{
//    public class ConsumerBehavior : IClassFixture<WebApplicationFactory<Startup>>
//    {
//        private readonly WebApplicationFactory<Startup> _appFactory;
//        private readonly ITestOutputHelper _output;

//        public ConsumerBehavior(WebApplicationFactory<Startup> appFactory, ITestOutputHelper output)
//        {
//            _appFactory = appFactory;
//            _output = output;
//        }

//        [Fact]
//        public void ShouldConsume()
//        {
//            //Arrange
//            var consumerLogic = new TestConsumerLogic();
//            var consumer = new MqConsumer<TestMsg,TestConsumerLogic>("foo-queue", consumerLogic);

//            var sp = new ServiceCollection()
//                .AddMqConsuming(cr => cr.RegisterConsumer(consumer))
//                .AddMqMsgEmulator()
//                .AddLogging(l => l.AddXUnit(_output))
//                .BuildServiceProvider();

//            var emulator = sp.GetService<IInputMessageEmulator>();

//            var testMsg = new TestMsg
//            {
//                Id = Guid.NewGuid()
//            };

//            //Act
//            emulator.Queue(testMsg, "foo-queue");

//            //Assert
//            Assert.Equal(testMsg.Id, consumerLogic.LastMsg.Id);
//        }

//        [Fact]
//        public async Task ShouldFailWhenOptionsNotSpecified()
//        {
//            //Arrange
//            //var client = _appFactory.WithWebHostBuilder(c =>
//            //        c.ConfigureServices(
//            //            s => s
//            //                .Configure<MqOptions>(o => o.Host = null)
//            //                .AddLogging(l => l.AddXUnit(_output)))
//            //        )
//            //    .CreateClient();
//            var consumerLogic = new TestConsumerLogic();
//            var consumer = new MqConsumer<TestMsg, TestConsumerLogic>("foo-queue", consumerLogic);

//            var host = Host.CreateDefaultBuilder()
//                .ConfigureServices(s => s
//                    .AddMqConsuming(cr => cr.RegisterConsumer(consumer))
//                    .AddLogging(l => l.AddXUnit(_output))
//                )
//                .Build();

//            await .StartAsync();

//            //Act

//            //Assert
//        }

//        [Fact]
//        public void ShouldEnableWhenOptionalAndOptionsSpecified()
//        {
//            //Arrange
//            var consumerLogic = new TestConsumerLogic();
//            var consumer = new MqConsumer<TestMsg, TestConsumerLogic>("foo-queue", consumerLogic);

//            var sp = new ServiceCollection()
//                .AddMqConsuming(cr => cr.RegisterConsumer(consumer), true)
//                .AddMqMsgEmulator()
//                .AddLogging(l => l.AddXUnit(_output))
//                .Configure<MqOptions>(o => o.Host = "http://localhost")
//                .BuildServiceProvider();

//            var emulator = sp.GetService<IInputMessageEmulator>();

//            var testMsg = new TestMsg
//            {
//                Id = Guid.NewGuid()
//            };

//            //Act
//            emulator.Queue(testMsg, "foo-queue");

//            //Assert
//            Assert.Equal(testMsg.Id, consumerLogic.LastMsg.Id);
//        }

//        class TestConsumerLogic : IMqConsumerLogic<TestMsg>
//        {
//            public TestMsg LastMsg { get; set; }

//            public Task Consume(MqMessage<TestMsg> message)
//            {
//                LastMsg = message.Payload;

//                return Task.CompletedTask;
//            }
//        }

//        class TestMsg
//        {
//            public Guid Id { get; set; }
//        }
//    }
//}
