using System;
using MyLab.Mq;
using MyLab.Mq.PubSub;
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
            var msgType = typeof(TestModelWithoutAttr);

            //Act
            var desc = MqMsgModelDesc.GetFromModel(msgType);

            //Assert
            Assert.Null(desc);
        }

        [Fact]
        public void ShouldProvideDesc()
        {
            //Arrange
            var msgType = typeof(TestModelWithAttr);

            //Act 
            var desc = MqMsgModelDesc.GetFromModel(msgType);

            //Assert
            Assert.NotNull(desc);
            Assert.Equal("foo", desc.Routing);
            Assert.Equal("bar", desc.Exchange);
        }

        class TestModelWithoutAttr
        {

        }

        [Mq(Routing = "foo", Exchange = "bar")]
        class TestModelWithAttr
        {

        }
    }
}
