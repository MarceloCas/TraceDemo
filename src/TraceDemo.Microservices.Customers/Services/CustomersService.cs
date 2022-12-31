using Grpc.Core;
using System.Diagnostics;
using TraceDemo.Microservices.Customers.RabbitMq;

namespace TraceDemo.Microservices.Customers.Services
{
    public class CustomersService : Customers.CustomersBase
    {
        private readonly ILogger<CustomersService> _logger;
        private readonly ActivitySource _activitySource;

        public CustomersService(
            ILogger<CustomersService> logger,
            ActivitySource activitySource
        )
        {
            _logger = logger;
            _activitySource = activitySource;
        }

        public override Task<GetCustomersReply> GetCustomersOperation(GetCustomersRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity(nameof(GetCustomersOperation));
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            var reply = new GetCustomersReply();

            reply.CustomerCollection.AddRange(
                Enumerable.Range(1, 10).Select(i =>
                    new CustomerDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Customer {i}",
                        BirthDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-i))
                    }
                )
            );

            return Task.FromResult(reply);
        }
        public override Task<AddCustomerReply> AddCustomerOperation(AddCustomerRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity(nameof(AddCustomerOperation));
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            RabbitMqPublisher.Publish(request.CustomerDto, _activitySource);

            return Task.FromResult(new AddCustomerReply());
        }
    }
}