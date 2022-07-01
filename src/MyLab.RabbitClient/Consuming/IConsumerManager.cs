using System.Threading;
using System.Threading.Tasks;

namespace MyLab.RabbitClient.Consuming
{
    interface IConsumerManager
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}