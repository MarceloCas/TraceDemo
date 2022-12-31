using System.Diagnostics;
using Grpc.Net.Client;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TraceDemo.Orchestrator.Services;

var serviceName = "TraceDemo.Orchestrator";
var serviceVersion = "1.0.0";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
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
        .AddGrpcClientInstrumentation()
        .AddAspNetCoreInstrumentation();
});
builder.Services.AddSingleton(serviceProvider => new ActivitySource(serviceName));
builder.Services.AddSingleton(serviceProvider => GrpcChannel.ForAddress("https://localhost:5005"));


builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<OrchestratorService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
