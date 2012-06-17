using System;
using System.Collections.Generic;

namespace Sample.Domain
{
    /// <summary>
    /// NB: this class is currently not wired to the infrastructure.
    /// See Lokad.CQRS Sample project for more details
    /// </summary>
    public class CustomerTransactionsProjection
    {
        readonly IDocumentWriter<CustomerId, CustomerTransactions> _store;
        public CustomerTransactionsProjection(IDocumentWriter<CustomerId, CustomerTransactions> store)
        {
            _store = store;
        }
        public void When(CustomerCreated e)
        {
            _store.Add(e.Id, new CustomerTransactions());
        }
        public void When(CustomerChargeAdded e)
        {
            _store.UpdateOrThrow(e.Id, v => v.AddTx(e.ChargeName, -e.Charge, e.NewBalance, e.TimeUtc));
        }
        public void When(CustomerPaymentAdded e)
        {
            _store.UpdateOrThrow(e.Id, v => v.AddTx(e.PaymentName, e.Payment, e.NewBalance, e.TimeUtc));
        }
    }
    [Serializable]
    public class CustomerTransactions
    {
        public IList<CustomerTransaction> Transactions = new List<CustomerTransaction>();
        public void AddTx(string name, CurrencyAmount change, CurrencyAmount balance, DateTime timeUtc)
        {
            Transactions.Add(new CustomerTransaction()
                {
                    Name = name,
                    Balance = balance,
                    Change = change,
                    TimeUtc = timeUtc
                });
        }
    }
    [Serializable]
    public class CustomerTransaction
    {
        public CurrencyAmount Change;
        public CurrencyAmount Balance;
        public string Name;
        public DateTime TimeUtc;
    }
}