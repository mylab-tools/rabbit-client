using System;

namespace MyLab.RabbitClient.Model
{
    /// <summary>
    /// Declare exchange types
    /// </summary>
    public enum RabbitExchangeType
    {
        Undefined,
        Direct,
        Topic,
        Fanout,
        Header
    }

    static class MqExchangeTypeExtensions
    {
        public static string ToLiteral(this RabbitExchangeType exchangeType)
        {
            switch (exchangeType)
            {
                case RabbitExchangeType.Direct:
                case RabbitExchangeType.Topic:
                case RabbitExchangeType.Fanout:
                    return exchangeType.ToString("G").ToLower();
                case RabbitExchangeType.Header:
                    return "headers";
                default:
                    throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, null);
            }
        }
    }
}