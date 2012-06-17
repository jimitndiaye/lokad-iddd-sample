using System;

namespace Sample.Domain
{
    public sealed class CustomerApplicationService
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

    }
}