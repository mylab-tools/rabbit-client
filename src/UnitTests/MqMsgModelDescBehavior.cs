using System;
using MyLab.MqApp;
using Xunit;

namespace UnitTests
{
    public class MqMsgModelDescBehavior
    {
        [Fact]
        public void ShouldFailWhenTargetObjectIsNull()
        {
            //Act & Assert
            Assert.Throws<ArgumentNullException>(() => MqMsgModelDesc.GetFromModel(null));
        }

        [Fact]
        public void ShouldFailWhenModelNotMarkedByQueueAttr()
        {
            //Arrange
            var msg = new TestModelWithoutAttr();

            //Act & Assert
            Assert.Throws<InvalidOperationException>(() => MqMsgModelDesc.GetFromModel(msg));

        }

        [Fact]
        public void ShouldProvideDesc()
        {
            //Arrange
            var msg = new TestModelWithAttr();

            //Act 
            var desc = MqMsgModelDesc.GetFromModel(msg);

            //Assert
            Assert.NotNull(desc);
            Assert.Equal("foo", desc.QueueName);
        }

        class TestModelWithoutAttr
        {

        }

        [Queue("foo")]
        class TestModelWithAttr
        {

        }
    }
}
