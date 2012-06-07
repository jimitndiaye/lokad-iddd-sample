using System;

namespace lokad_iddd_sample
{
    public static class Program
    {
        public static void Main()
        {
            //var conn = "Data Source=.\\SQLExpress;Initial Catalog=lokadsalescast_samples;Integrated Security=true";
            //var store = new SqlEventStore(conn, new SampleStrategy());
            var store = new FileEventStore(new SampleStrategy(), "temp");
            store.Initialize();
            var service = new CustomerApplicationService(store);

            service.When(new CreateCustomer(){ Id = new CustomerId(12), Name = "Lokad"});
            service.When(new RenameCustomer() { Id = new CustomerId(12), NewName = "Lokad SAS"});
        }

    }

    public sealed class CustomerId : IIdentity
    {
        public readonly long Id;

        public CustomerId(long id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return string.Format("customer-{0}", Id);
        }
    }

    

    [Serializable]
    public class CustomerCreated : IEvent
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
    }

    public sealed class CreateCustomer :ICommand
    {
        public CustomerId Id { get; set; }
        public string Name { get; set; }
    }

    public class RenameCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string NewName { get; set; }
    }
    [Serializable]
    public class CustomerRenamed : IEvent
    {
        public string Name { get; set; }
    }
}