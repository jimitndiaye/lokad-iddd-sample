using System;
using System.Collections.Generic;

namespace Sample.Domain
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


        public void Create(CustomerId id, string name, Currency currency)
        {
            if (_state.Created)
                throw new InvalidOperationException("Customer was already created");
            Apply(new CustomerCreated
                {
                    Created = DateTime.UtcNow,
                    Name = name,
                    Id = id,
                    Currency = currency
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

        public void LockCustomer(string reason)
        {
            if (_state.ConsumptionLocked)
                return;
            
            Apply(new CustomerLocked
                {
                    Id = _state.Id,
                    Reason = reason
                });
        }

        public void LockForAccountOverdraft(IPricingModel model, string comment)
        {
            if (_state.ManualBilling) return;
            var balance = model.GetOverdraftThreshold(_state.Currency);
            if (_state.Balance < balance)
            {
                LockCustomer("Overdraft. " + comment);
            }

        }

        public void AddPayment(string name, CurrencyAmount amount)
        {
            Apply(new CustomerPaymentAdded()
                {
                    Id = _state.Id,
                    Payment = amount,
                    NewBalance = _state.Balance + amount,
                    PaymentName = name,
                    Transaction = _state.MaxTransactionId + 1,
                    TimeUtc = DateTime.UtcNow
                });
        }

        public void Charge(string name, CurrencyAmount amount)
        {
            Apply(new CustomerChargeAdded()
                {
                    Id = _state.Id,
                    Charge = amount,
                    NewBalance = _state.Balance - amount,
                    ChargeName = name,
                    Transaction = _state.MaxTransactionId + 1,
                    TimeUtc = DateTime.UtcNow
                });
        }
    }
}