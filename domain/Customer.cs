using System;
using System.Collections.Generic;

namespace lokad_iddd_sample
{
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


        public void Create(CustomerId id, string name)
        {
            if (_state.Created)
                throw new InvalidOperationException("Customer was already created");
            Apply(new CustomerCreated()
                {
                    Created = DateTime.UtcNow,
                    Name = name,
                    Id = id
                });
        }
        public void Rename(string name)
        {
            if (_state.Name == name)
                return;
            Apply(new CustomerRenamed
                {
                    Name = name,
                    Id = _state.Id,
                    OldName = _state.Name,
                    Renamed = DateTime.UtcNow
                });
        }
    }
}