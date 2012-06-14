using System;

namespace lokad_iddd_sample
{
    public sealed class CustomerApplicationService
    {
        readonly IEventStore _eventStore;
        public CustomerApplicationService(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }


        void Update(CustomerId id, Action<Customer> execute)
        {
            EventStream eventStream = _eventStore.LoadEventStream(id,0, int.MaxValue);
            Customer customer = new Customer(eventStream.Events);
            execute(customer);
            _eventStore.AppendToStream(id, eventStream.Version, customer.Changes);
        }

        public void When(CreateCustomer c)
        {
            Update(c.Id, a => a.Create(c.Name));
        }
        public void When(RenameCustomer c)
        {
            Update(c.Id, a=> a.Rename(c.NewName));
        }
    }
}