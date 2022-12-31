using Grpc.Core;
using Grpc.Net.Client;
using System.Diagnostics;

namespace TraceDemo.Orchestrator.Services
{
    public class OrchestratorService : Orchestrator.Customers.CustomersBase
    {
        private readonly ILogger<OrchestratorService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly Microservices.Customers.Customers.CustomersClient _microservicesCustomerClient;
        public OrchestratorService(
            ILogger<OrchestratorService> logger,
            ActivitySource activitySource,
            GrpcChannel grpcChannel
        )
        {
            _logger = logger;
            _activitySource = activitySource;
            _microservicesCustomerClient = new Microservices.Customers.Customers.CustomersClient(grpcChannel);
        }

        public async override Task<GetCustomersReply> GetCustomersOperation(GetCustomersRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity(nameof(GetCustomersOperation));
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            var reply = new GetCustomersReply();

            var microserviceCustomerReply = await _microservicesCustomerClient.GetCustomersOperationAsync(
                new Microservices.Customers.GetCustomersRequest(),
                cancellationToken: context.CancellationToken
            );

            reply.CustomerCollection.AddRange(
                microserviceCustomerReply.CustomerCollection.Select(q =>
                    new CustomerDto
                    {
                        Id = q.Id,
                        Name = q.Name,
                        BirthDate = q.BirthDate
                    }
                )
            );

            return reply;
        }

        public async override Task<AddCustomerReply> AddCustomerOperation(AddCustomerRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity(nameof(AddCustomerOperation));
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            var microserviceCustomerReply = await _microservicesCustomerClient.AddCustomerOperationAsync(
                new Microservices.Customers.AddCustomerRequest
                {
                    CustomerDto = new Microservices.Customers.CustomerDto
                    {
                        Id = request.CustomerDto.Id,
                        Name = request.CustomerDto.Name,
                        BirthDate = request.CustomerDto.BirthDate
                    }
                },
                cancellationToken: context.CancellationToken
            );

            return new AddCustomerReply();
        }
    }
}