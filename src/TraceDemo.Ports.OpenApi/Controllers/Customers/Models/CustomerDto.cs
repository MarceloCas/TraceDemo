namespace TraceDemo.Ports.OpenApi.Controllers.Customers.Models
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? BirthDate { get; set; }

        public CustomerDto()
        {
            Name = string.Empty;
        }
    }
}
