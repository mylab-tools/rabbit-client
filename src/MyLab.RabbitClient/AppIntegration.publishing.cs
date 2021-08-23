using MyLab.RabbitClient.Publishing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AppIntegration
    {
        /// <summary>
        /// Add publisher service
        /// </summary>
        public static IServiceCollection AddRabbitPublisher(this IServiceCollection srv)
        {
            return srv
                .TryAddCommon()
                .AddSingleton<IRabbitPublisher, RabbitPublisher>();
        }
    }
}