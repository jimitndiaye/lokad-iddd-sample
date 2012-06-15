using System;
using System.Collections.Generic;
using System.IO;
using Sample.Domain;
using Sample.Storage;
using Sample.StorageImplementations.Files;
using Sample.StorageImplementations.MsSql;

namespace Sample
{
    public static class Program
    {
        public static void Main()
        {
            if (File.Exists("Readme.md"))
                Console.WriteLine(File.ReadAllText("Readme.md"));
            
            // persistence
            var store = CreateFileStoreForTesting();
            var events = new EventStore(store);

            // various domain services
            var pricing = new PricingModel();

            var server = new Server();
            server.Handlers.Add(new CustomerApplicationService(events, pricing));

            server.Dispatch(new CreateCustomer { Id = new CustomerId(12), Name = "Lokad", Currency = Currency.Eur});
            server.Dispatch(new RenameCustomer { Id = new CustomerId(12), NewName = "Lokad SAS"});
            server.Dispatch(new ChargeCustomer { Id = new CustomerId(12), Amount = 20m.Eur(), Name = "Forecasting"});
            
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
                Console.WriteLine();
                Console.WriteLine("Wiping even store for demo purposes");
                Console.WriteLine();
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
                Console.ForegroundColor= ConsoleColor.DarkCyan;
                Console.WriteLine("Command: " + cmd);
                Console.ForegroundColor=ConsoleColor.DarkGray;
                foreach (var handler in Handlers)
                {
                    ((dynamic) handler).When((dynamic) cmd);
                }
            }

            public readonly IList<object> Handlers = new List<object>(); 
        }
    }
}