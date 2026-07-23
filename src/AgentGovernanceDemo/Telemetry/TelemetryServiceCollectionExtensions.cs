// EN: Registers demo tracing and metrics, exporting them to Azure Monitor only when configuration is valid.
// JA: デモのトレースとメトリックを登録し、構成が有効な場合だけ Azure Monitor へ出力します。

using System.Reflection;
using AgentGovernance.Telemetry;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AgentGovernanceDemo.Telemetry;

/// <summary>
/// EN: Provides dependency-injection registration for OpenTelemetry and Azure Monitor export.<br/>
/// JA: OpenTelemetry と Azure Monitor 出力の依存関係注入登録を提供します。
/// </summary>
public static class TelemetryServiceCollectionExtensions
{
    public const string DefaultServiceName = "agent-governance-demo";

    /// <summary>
    /// EN: Adds telemetry services and conditionally enables Azure Monitor export.<br/>
    /// JA: テレメトリサービスを追加し、条件を満たす場合に Azure Monitor 出力を有効化します。
    /// </summary>
    public static IServiceCollection AddAgentGovernanceDemoTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var serviceVersion = GetServiceVersion();
        var connectionString = GetConnectionString(configuration);
        var state = GetState(connectionString);
        var status = new TelemetryStatus(
            state,
            DefaultServiceName,
            serviceVersion,
            environment.EnvironmentName,
            GetStatusMessage(state));

        services.AddSingleton(status);
        services.AddSingleton<DemoTelemetry>();

        if (state != TelemetryState.Configured)
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(DefaultServiceName, serviceVersion: serviceVersion)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment.name", environment.EnvironmentName)
                ]))
            .WithTracing(tracing => tracing.AddSource(DemoTelemetry.ActivitySourceName))
            .WithMetrics(metrics => metrics.AddMeter(
                DemoTelemetry.MeterName,
                GovernanceMetrics.MeterName))
            .UseAzureMonitor(options => options.ConnectionString = connectionString);

        return services;
    }

    private static string? GetConnectionString(IConfiguration configuration)
    {
        var value = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = configuration["ApplicationInsights:ConnectionString"];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static TelemetryState GetState(string? connectionString)
    {
        if (connectionString is null)
        {
            return TelemetryState.Disabled;
        }

        return HasInstrumentationKey(connectionString)
            ? TelemetryState.Configured
            : TelemetryState.Degraded;
    }

    private static bool HasInstrumentationKey(string connectionString)
    {
        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            if (segment[..separator].Trim().Equals("InstrumentationKey", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(segment[(separator + 1)..].Trim(), out _))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetServiceVersion()
    {
        var assembly = typeof(TelemetryServiceCollectionExtensions).Assembly;
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    private static string GetStatusMessage(TelemetryState state) =>
        state switch
        {
            TelemetryState.Configured => "Azure Monitor telemetry is configured.",
            TelemetryState.Degraded => "Azure Monitor telemetry is disabled because its connection string is invalid.",
            _ => "Azure Monitor telemetry is disabled because no connection string is configured."
        };
}
