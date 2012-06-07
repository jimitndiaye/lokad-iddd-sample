using System;
using System.Collections.Generic;
using System.Linq;

namespace lokad_iddd_sample
{
    public static class Program
    {
        public static void Main()
        {
            var conn = "Data Source=.\\SQLExpress;Initial Catalog=lokadsalescast_samples;Integrated Security=true";
            var store = new SqlEventStore(conn, new SampleStrategy());
            //var store = new FileEventStore(new SampleStrategy(), "temp");
            store.Initialize();

            var server = new Server();
            server.Handlers.Add(new CustomerApplicationService(store));

            server.Dispatch(new CreateCustomer { Id = new CustomerId(12), Name = "Lokad"});
            server.Dispatch(new RenameCustomer { Id = new CustomerId(12), NewName = "Lokad SAS"});
        }

        public sealed class Server
        {
            public void Dispatch(ICommand cmd)
            {
                Console.WriteLine(cmd);
                foreach (var handler in Handlers)
                {
                    ((dynamic) handler).When((dynamic) cmd);
                }
            }

            public readonly IList<object> Handlers = new List<object>(); 
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
    [Serializable]
    public sealed class CreateCustomer :ICommand
    {
        public CustomerId Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("Create {0} named '{1}'", Id, Name);
        }
    }

    public class RenameCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string NewName { get; set; }

        public override string ToString()
        {
            return string.Format("Rename {0} to '{1}'", Id, NewName);
        }
    }
    [Serializable]
    public class CustomerRenamed : IEvent
    {
        public string Name { get; set; }
    }
}