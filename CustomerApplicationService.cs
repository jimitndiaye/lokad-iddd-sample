using System;
using System.Collections.Generic;

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

    public class Customer
    {

        public readonly IList<IEvent> Changes = new List<IEvent>();
        readonly CustomerState _state;
        public Customer(IEnumerable<IEvent> events)
        {
            _state = new CustomerState(events);
        }

        void Apply(IEvent e)
        {
            _state.Mutate(e);
            Changes.Add(e);
        }


        public void Create(string name)
        {
            if (_state.Created)
                throw new InvalidOperationException("Customer was already created");
            Apply(new CustomerCreated()
                {
                    Created = DateTime.UtcNow,
                    Name = name
                });
        }
        public void Rename(string name)
        {
            if (_state.Name == name)
                return;
            Apply(new CustomerRenamed
                {
                    Name = name
                });
        }
    }

    public class CustomerState
    {

        public string Name { get; private set; }
        public bool Created { get; private set; }
        public CustomerState(IEnumerable<IEvent> events)
        {
            foreach (var e in events)
            {
                Mutate(e);
            }
        }

        public void When(CustomerCreated e)
        {
            Created = true;
            Name = e.Name;
        }

        public void When(CustomerRenamed e)
        {
            Name = e.Name;
        }
         

        public void Mutate(IEvent e)
        {
            ((dynamic) this).When((dynamic)e);
        }
        
    }
}