using System;
using System.ComponentModel.Design;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Mq;
using Xunit;

namespace FuncTests
{
    public class InputMessageEmulatorBehavior
    {
        [Fact]
        public async Task ShouldSendFakeMessage()
        {
            //Arrange
            
            var services = new ServiceCollection();

            var logic = new TestConsumerLogic();
            var consumer = new MqConsumer<TestEntity, TestConsumerLogic>("foo-queue", logic);
            var emulatorRegistrar = new InputMessageEmulatorRegistrar();

            services.AddMqConsuming(
                consumerRegistrar => consumerRegistrar.RegisterConsumer(consumer),
                emulatorRegistrar
                );

            var srvProvider = services.BuildServiceProvider();

            var emulator = srvProvider.GetService<IInputMessageEmulator>();
            
            var testEntity = new TestEntity { Id = Guid.NewGuid().ToString()};

            //Act
            var res = await emulator.Queue(testEntity, "foo-queue");

            //Assert
            Assert.True(res.Acked);
            Assert.False(res.Rejected);
            Assert.Null(res.RejectionException);
            Assert.False(res.RequeueFlag);
            Assert.Equal(testEntity.Id, logic.LastMsgId);
        }

        [Fact]
        public async Task ShouldGetNegativeResultWhenProcessingError()
        {
            //Arrange

            var services = new ServiceCollection();

            var testException = new Exception();
            var logic = new TestConsumerLogicWithError(testException);
            var requeueFlag = true;
            var consumer = new MqConsumer<TestEntity, TestConsumerLogicWithError>("foo-queue", logic)
            {
                RequeueWhenError = requeueFlag
            };

            var emulatorRegistrar = new InputMessageEmulatorRegistrar();

            services.AddMqConsuming(
                consumerRegistrar => consumerRegistrar.RegisterConsumer(consumer),
                emulatorRegistrar
            );

            var srvProvider = services.BuildServiceProvider();

            var emulator = srvProvider.GetService<IInputMessageEmulator>();

            var testEntity = new TestEntity { Id = Guid.NewGuid().ToString() };

            //Act
            var res = await emulator.Queue(testEntity, "foo-queue");

            //Assert
            Assert.False(res.Acked);
            Assert.True(res.Rejected);
            Assert.Equal(testException, res.RejectionException);
            Assert.Equal(requeueFlag, res.RequeueFlag);
            Assert.Equal(testEntity.Id, logic.LastMsgId);
        }

        [Fact]
        public async Task ShouldProvideDiForLogic()
        {
            //Arrange

            var services = new ServiceCollection();

            var consumer = new MqConsumer<TestEntity, TestConsumerLogicWithDependency>("foo-queue");
            var emulatorRegistrar = new InputMessageEmulatorRegistrar();

            services.AddSingleton<TestConsumerLogicWithDependency.Dependency2>();
            services.AddSingleton<TestConsumerLogicWithDependency.Dependency1>();
            services.AddMqConsuming(
                consumerRegistrar => consumerRegistrar.RegisterConsumer(consumer),
                emulatorRegistrar
            );

            var srvProvider = services.BuildServiceProvider();

            var emulator = srvProvider.GetService<IInputMessageEmulator>();

            var testEntity = new TestEntity { Id = Guid.NewGuid().ToString() };

            //Act
            var res = await emulator.Queue(testEntity, "foo-queue");

            //Assert
            Assert.True(res.Acked);
        }

        class TestEntity
        {
            public string Id { get; set; }
        }

        class TestConsumerLogicWithDependency : IMqConsumerLogic<TestEntity>
        {
            public TestConsumerLogicWithDependency(Dependency1 dependency)
            {
                
            }

            public Task Consume(MqMessage<TestEntity> message)
            {
                

                return Task.CompletedTask;
            }

            
            public class Dependency1
            {
                public Dependency1(Dependency2 dep2)
                {
                    
                }
            }

            public class Dependency2
            {

            }
        }

        class TestConsumerLogic : IMqConsumerLogic<TestEntity>
        {
            public string LastMsgId { get; set; }

            public Task Consume(MqMessage<TestEntity> message)
            {
                LastMsgId = message.Payload.Id;

                return Task.CompletedTask;
            }
        }

        class TestConsumerLogicWithError : IMqConsumerLogic<TestEntity>
        {
            private readonly Exception _exToThrow;

            public string LastMsgId { get; set; }

            public TestConsumerLogicWithError(Exception exToThrow)
            {
                _exToThrow = exToThrow;
            }

            public Task Consume(MqMessage<TestEntity> message)
            {
                LastMsgId = message.Payload.Id;
                throw _exToThrow;
            }
        }
    }
}
