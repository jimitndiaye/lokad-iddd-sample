using System;
using Sample.Storage;

namespace Sample.Domain
{
    public sealed class CustomerApplicationService : IApplicationService
    {
        // event store for accessing event streams
        readonly IEventStore _eventStore;
        // domain service that is neeeded by aggregate
        readonly IPricingService _pricingService;

        // pass dependencies for this application service via constructor
        public CustomerApplicationService(IEventStore eventStore, IPricingService pricingService)
        {
            _eventStore = eventStore;
            _pricingService = pricingService;
        }


        public void When(CreateCustomer c)
        {
            Update(c.Id, a => a.Create(c.Id,c.Name, c.Currency, _pricingService));
        }

        public void When(RenameCustomer c)
        {
            Update(c.Id, a=> a.Rename(c.NewName));
        }

        public void When(AddCustomerPayment c)
        {
            Update(c.Id, a => a.AddPayment(c.Name, c.Amount));
        }

        public void When(ChargeCustomer c)
        {
            Update(c.Id, a => a.Charge(c.Name, c.Amount));
        }

        public void When(LockCustomerForAccountOverdraft c)
        {
            Update(c.Id, a => a.LockForAccountOverdraft(c.Comment, _pricingService));
        }

        public void When(LockCustomer c)
        {
            Update(c.Id, a => a.LockCustomer(c.Reason));
        }


        // method with direct call, as illustrated in the IDDD Book

        // Step 1: LockCustomerForAccountOverdraft method of 

        // Customer Application Service is called  

        public void LockCustomerForAccountOverdraft(CustomerId customerId, string comment)
        {
            // Step 2.1: Load event stream for Customer, given its id
            var stream = _eventStore.LoadEventStream(customerId);
            // Step 2.2: Build aggregate from event stream
            var customer = new Customer(stream.Events);
            // Step 3: call aggregate method, passing it arguments and pricing domain service
            customer.LockForAccountOverdraft(comment, _pricingService);
            // Step 4: commit changes to the event stream by id 
            _eventStore.AppendToStream(customerId, stream.Version, customer.Changes);
        }

        public void Execute(ICommand cmd)
        {
            // pass command to a specific method named when
            // that can handle the command
            ((dynamic)this).When((dynamic)cmd);
        }

        void Update(CustomerId id, Action<Customer> execute)
        {
            // Load event stream from the store
            EventStream stream = _eventStore.LoadEventStream(id);
            // create new Customer aggregate from the history
            Customer customer = new Customer(stream.Events);
            // execute delegated action
            execute(customer);
            // append resulting changes to the stream
            _eventStore.AppendToStream(id, stream.Version, customer.Changes);
        }
        // Sample of method that would apply simple conflict resolution.
        // see IDDD book or Greg's videos for more in-depth explanation  
        void UpdateWithSimpleConflictResolution(CustomerId id, Action<Customer> execute)
        {
            while (true)
            {
                EventStream eventStream = _eventStore.LoadEventStream(id);
                Customer customer = new Customer(eventStream.Events);
                execute(customer);

                try
                {
                    _eventStore.AppendToStream(id, eventStream.Version, customer.Changes);
                    return;
                }
                catch (OptimisticConcurrencyException ex)
                {
                    foreach (var clientEvent in customer.Changes)
                    {
                        foreach (var actualEvent in ex.ActualEvents)
                        {
                            if (ConflictsWith(clientEvent, actualEvent))
                            {
                                var msg = string.Format("Conflict between {0} and {1}", 
                                    clientEvent, actualEvent);
                                throw new RealConcurrencyException(msg, ex);
                            }
                        }
                    }
                    // there are no conflicts and we can append
                    _eventStore.AppendToStream(id, ex.ActualVersion, customer.Changes);
                }
            }
        }

        static bool ConflictsWith(IEvent x, IEvent y)
        {
            return x.GetType() == y.GetType();
        }
    }
}