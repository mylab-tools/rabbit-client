using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyLab.RabbitClient.Connection;

namespace MyLab.RabbitClient.HealthCheck
{
    class RabbitConnectionHealthCheck : IHealthCheck
    {
        private readonly IRabbitConnectionProvider _connectionProvider;
        private readonly RabbitOptions _opt;

        public RabbitConnectionHealthCheck(IRabbitConnectionProvider connectionProvider, IOptions<RabbitOptions> options)
        {
            _connectionProvider = connectionProvider;
            _opt = options.Value;
        }

        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            bool hasConnection;
            Exception exception = null;

            try
            {
                hasConnection = _connectionProvider.Provide() != null;
            }
            catch (RabbitNotConnectedException)
            {
                hasConnection = false;
            }
            catch (Exception e)
            {
                hasConnection = false;
                exception = e;
            }

            string description = hasConnection
                ? "Connection established"
                : "No connection established";
            
            var check = new HealthCheckResult(
                hasConnection
                    ? HealthStatus.Healthy
                    : HealthStatus.Unhealthy,
                description,
                exception,
                new Dictionary<string, object>
                {
                    {"connection-string", $"{_opt.User ?? "[null]"}@{_opt.Host ?? "[null]"}:{_opt.Port}/{_opt.VHost ?? "[null]"}"}
                }
            );

            return Task.FromResult(check);
        }
    }
}
