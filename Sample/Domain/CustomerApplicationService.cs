using System;

namespace Sample.Domain
{
    public sealed class CustomerApplicationService
    {
        readonly IEventStore _eventStore;
        readonly IPricingModel _pricingModel;
        public CustomerApplicationService(IEventStore eventStore, IPricingModel pricingModel)
        {
            _eventStore = eventStore;
            _pricingModel = pricingModel;
        }


        void Update(CustomerId id, Action<Customer> execute)
        {
            // Load event stream from the store
            EventStream stream = _eventStore.LoadEventStream(id,0, int.MaxValue);
            // create new Customer aggregate from the history
            Customer customer = new Customer(stream.Events);
            // execute delegated action
            execute(customer);
            // append resulting changes to the stream
            _eventStore.AppendToStream(id, stream.Version, customer.Changes);
        }

        public void When(CreateCustomer c)
        {
            Update(c.Id, a => a.Create(c.Id,c.Name, c.Currency, _pricingModel));
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
            Update(c.Id, a => a.LockForAccountOverdraft(_pricingModel, c.Comment));
        }

        public void When(LockCustomer c)
        {
            Update(c.Id, a => a.LockCustomer(c.Reason));
        }

       

    }


    
}