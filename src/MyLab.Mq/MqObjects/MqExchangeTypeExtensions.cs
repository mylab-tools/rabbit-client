using System;

namespace MyLab.Mq.MqObjects
{
    static class MqExchangeTypeExtensions
    {
        public static string ToLiteral(this MqExchangeType exchangeType)
        {
            switch (exchangeType)
            {
                case MqExchangeType.Direct:
                case MqExchangeType.Topic:
                case MqExchangeType.Fanout:
                    return exchangeType.ToString("G").ToLower();
                case MqExchangeType.Header:
                    return "headers";
                default:
                    throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, null);
            }
        }
    }
}