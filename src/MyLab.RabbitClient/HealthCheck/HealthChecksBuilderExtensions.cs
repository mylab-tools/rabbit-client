using MyLab.RabbitClient.HealthCheck;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for <see cref="IHealthChecksBuilder"/>
    /// </summary>
    public static class HealthChecksBuilderExtensions
    {
        /// <summary>
        /// Adds Rabbit checks
        /// </summary>
        public static IHealthChecksBuilder AddRabbit(this IHealthChecksBuilder hCh)
        {
            return hCh.AddCheck<RabbitConnectionHealthCheck>("rabbit");
        }
    }
}
