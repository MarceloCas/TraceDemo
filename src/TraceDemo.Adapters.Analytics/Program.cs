using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using TraceDemo.Adapters.Analytics;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        var serviceName = "TraceDemo.Adapters.Analytics";
        var serviceVersion = "1.0.0";

        services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddOtlpExporter(opt =>
                {
                    opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                })
                .AddSource(serviceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();
        });
        services.AddSingleton(serviceProvider => new ActivitySource(serviceName));

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
