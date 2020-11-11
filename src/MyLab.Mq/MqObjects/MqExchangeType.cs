namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Declare exchange types
    /// </summary>
    public enum MqExchangeType
    {
        Undefined,
        Direct,
        Topic,
        Fanout,
        Header
    }
}