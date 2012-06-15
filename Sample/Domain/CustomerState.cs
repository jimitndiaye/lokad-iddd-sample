using System.Collections.Generic;

namespace Sample.Domain
{
    public class CustomerState
    {

        public string Name { get; private set; }
        public bool Created { get; private set; }
        public CustomerId Id { get; private set; }
        public bool ConsumptionLocked { get; private set; }
        public bool ManualBilling { get; private set; }
        public Currency Currency { get; private set; }
        public CurrencyAmount Balance { get; private set; }

        public int MaxTransactionId { get; private set; }

        public CustomerState(IEnumerable<IEvent> events)
        {
            foreach (var e in events)
            {
                Mutate(e);
            }
        }

        public void When(CustomerLocked e)
        {
            ConsumptionLocked = true;
        }

        public void When(CustomerPaymentAdded e)
        {
            Balance = e.NewBalance;
            MaxTransactionId = e.Transaction;
        }
        public void When(CustomerChargeAdded e)
        {
            Balance = e.NewBalance;
            MaxTransactionId = e.Transaction;
        }

        public void When(CustomerCreated e)
        {
            Created = true;
            Name = e.Name;
            Id = e.Id;
            Currency = e.Currency;
            Balance = new CurrencyAmount(0, e.Currency);
        }

        public void When(CustomerRenamed e)
        {
            Name = e.Name;
        }

        public void Mutate(IEvent e)
        {
            // redirect event to one of the 'When' handlers
            // via .NET magic
            ((dynamic) this).When((dynamic)e);
        }
    }
}