using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace lokad_iddd_sample
{
    public static class Program
    {
        public static void Main()
        {
            var store = CreateFileStoreForTesting();
            var events = new EventStore(store);
            //var store = new FileEventStore(new SampleStrategy(), "temp");
            

            var server = new Server();
            server.Handlers.Add(new CustomerApplicationService(events));

            server.Dispatch(new CreateCustomer { Id = new CustomerId(12), Name = "Lokad"});
            server.Dispatch(new RenameCustomer { Id = new CustomerId(12), NewName = "Lokad SAS"});

            Console.WriteLine("Press any key to continue");
            Console.ReadKey(true);
        }

        static IAppendOnlyStore CreateSqlStore()
        {
            var conn = "Data Source=.\\SQLExpress;Initial Catalog=lokadsalescast_samples;Integrated Security=true";
            var store = new SqlAppendOnlyStore(conn);
            store.Initialize();
            return store;
        }
        static IAppendOnlyStore CreateFileStoreForTesting()
        {
            {
                // reset the store, since we are testing
            }
            var combine = Path.Combine(Directory.GetCurrentDirectory(), "store");
            if (Directory.Exists(combine))
            {
                Console.WriteLine("Wiping even store for demo purposes");
                Directory.Delete(combine, true);
            }
            var store = new FileAppendOnlyStore(combine);
            store.Initialize();
            return store;
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

    
    [Serializable]
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
        public CustomerId Id { get; set; }

        public override string ToString()
        {
            return string.Format("Customer {0} created", Name);
        }
        
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
        // normally you don't need old name. But here, 
        // we include it just for demo
        public string OldName { get; set; }
        public CustomerId Id { get; set; }
        public DateTime Renamed { get; set; }

        public override string ToString()
        {
            return string.Format("Customer renamed from '{0}' to '{1}'", OldName, Name);
        }
    }
}