using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace TraceDemo.Adapters.Analytics.RabbitMq
{
    public class RabbitMqConsumer
    {
        private readonly ActivitySource _activitySource;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqConsumer(ActivitySource activitySource)
        {
            _activitySource = activitySource;
        }

        public void TryStartConsumer(Func<string, BasicDeliverEventArgs, Task<(bool success, bool requeue)>> handler)
        {
            if (_channel?.IsOpen == true)
                return;

            _channel?.Dispose();
            _connection?.Dispose();

            var factory = new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "trace-demo",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var propagator = Propagators.DefaultTextMapPropagator;
                var parentContext = propagator.Extract(default, ea.BasicProperties, (props, key) =>
                {
                    if (props.Headers.TryGetValue(key, out var value))
                    {
                        var bytes = value as byte[];
                        return new[] { Encoding.UTF8.GetString(bytes) };
                    }

                    return Enumerable.Empty<string>();
                });
                Baggage.Current = parentContext.Baggage;

                var activity = _activitySource.StartActivity("Process Message", ActivityKind.Consumer, parentContext.ActivityContext);
                activity?.SetTag("messaging.system", "rabbitmq");
                activity?.SetTag("messaging.destination_kind", "queue");
                activity?.SetTag("messaging.rabbitmq.queue", "sample");

                var handlerResult = await handler(
                    Encoding.UTF8.GetString(ea.Body.ToArray()),
                    ea
                );

                if (handlerResult.success)
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                else
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, handlerResult.requeue);
            };

            _channel.BasicConsume(
                queue: "trace-demo",
                autoAck: false,
                consumer
            );
        }
    }
}
