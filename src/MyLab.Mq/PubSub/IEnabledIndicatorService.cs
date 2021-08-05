using Microsoft.Extensions.Options;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Indicates MQ enable
    /// </summary>
    public interface IEnabledIndicatorService
    {
        /// <summary>
        /// Gets whether MQ tools should be enabled 
        /// </summary>
        /// <returns></returns>
        bool ShouldBeEnabled();
    }

    class EnabledIndicatorService : IEnabledIndicatorService
    {
        private readonly MqOptions _options;

        public EnabledIndicatorService(IOptions<MqOptions> options)
            :this(options.Value)
        {
        }

        public EnabledIndicatorService(MqOptions options)
        {
            _options = options;
        }

        public bool ShouldBeEnabled()
        {
            return _options == null || _options.Host != null;
        }
    }
}
