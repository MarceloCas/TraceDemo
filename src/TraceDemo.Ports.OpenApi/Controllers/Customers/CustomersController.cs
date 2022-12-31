using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TraceDemo.Ports.OpenApi.Controllers.Customers.Models;

namespace TraceDemo.Ports.OpenApi.Controllers.Customers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ILogger<CustomersController> _logger;
        private readonly ActivitySource _activitySource;
        private readonly Orchestrator.Customers.CustomersClient _orchestratorCustomerClient;

        public CustomersController(
            ILogger<CustomersController> logger,
            ActivitySource activitySource,
            GrpcChannel grpcChannel
        )
        {
            _logger = logger;
            _activitySource = activitySource;
            _orchestratorCustomerClient = new Orchestrator.Customers.CustomersClient(grpcChannel); ;
        }

        [HttpGet]
        public async Task<IEnumerable<CustomerDto>> GetCustomers(CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(nameof(GetCustomers));
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            var reply = await _orchestratorCustomerClient.GetCustomersOperationAsync(
                new Orchestrator.GetCustomersRequest(),
                cancellationToken: cancellationToken
            );

            return reply.CustomerCollection.Select(q =>
                new CustomerDto
                {
                    Id = Guid.Parse(q.Id),
                    Name = q.Name,
                    BirthDate = q.BirthDate.ToDateTimeOffset()
                }
            );
        }

        [HttpPost]
        public async Task AddCustomer([FromBody] CustomerDto customerDto, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(nameof(AddCustomer));
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            var orchestratorCustomerDto = new Orchestrator.CustomerDto
            {
                Id = customerDto.Id.ToString(),
                Name = customerDto.Name
            };

            if (customerDto.BirthDate != null)
                orchestratorCustomerDto.BirthDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(customerDto.BirthDate.Value);

            await _orchestratorCustomerClient.AddCustomerOperationAsync(
                new Orchestrator.AddCustomerRequest
                {
                    CustomerDto = orchestratorCustomerDto,
                },
                cancellationToken: cancellationToken
            );
        }
    }
}