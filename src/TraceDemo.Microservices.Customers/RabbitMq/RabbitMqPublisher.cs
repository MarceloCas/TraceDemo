using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Diagnostics;

namespace TraceDemo.Microservices.Customers.RabbitMq
{
    public static class RabbitMqPublisher
    {
        private static IConnection _connection;
        private static IModel _channel;

        private static void TryOpenConnection()
        {
            if (_channel?.IsOpen == true)
                return;

            _channel?.Dispose();
            _connection?.Dispose();

            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "trace-demo",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public static void Publish<T>(T message, ActivitySource activitySource)
        {
            using var activity = activitySource.StartActivity($"Publish Message", ActivityKind.Producer);
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            TryOpenConnection();

            var props = _channel.CreateBasicProperties();

            var propagator = Propagators.DefaultTextMapPropagator;

            if(activity != null)
                propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, (props, key, value) =>
                {
                    props.Headers ??= new Dictionary<string, object>();
                    props.Headers[key] = value;
                });

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.rabbitmq.queue", "trace-demo");

            _channel.BasicPublish(
                exchange: "",
                routingKey: "trace-demo",
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))
            );
        }
    }
}
