using System;

namespace Sample.Domain
{
    [Serializable]
    public sealed class CustomerId : IIdentity
    {
        public readonly long Id;

        public CustomerId(long id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return string.Format("customer-{0}", Id);
        }
    }

    public enum Currency
    {
        None,
        Eur,
        Usd,
        Rur
    }

    public static class CurrencyExtension
{
    public static CurrencyAmount Eur(this decimal amount)
    {
        return new CurrencyAmount(amount, Currency.Eur);
    }
}
    [Serializable]
    public struct CurrencyAmount
    {
        public readonly decimal Amount;
        public readonly Currency Currency;

        public CurrencyAmount(decimal amount, Currency currency)
        {
            Amount = amount;
            Currency = currency;
        }


        
        public static bool operator ==(CurrencyAmount left, CurrencyAmount right)
        {
            left.CheckCurrency(right.Currency, "==");
            return left.Amount == right.Amount;
        }

        public static bool operator !=(CurrencyAmount left, CurrencyAmount right)
        {
            left.CheckCurrency(right.Currency, "!=");
            return left.Amount != right.Amount;
        }
        public static bool operator < (CurrencyAmount left, CurrencyAmount right)
        {
            left.CheckCurrency(right.Currency, "<");
            return left.Amount < right.Amount;
        }

        public static CurrencyAmount operator + (CurrencyAmount left, CurrencyAmount right)
        {
            left.CheckCurrency(right.Currency, "+");
            return new CurrencyAmount(left.Amount + right.Amount, left.Currency);
        }
        public static CurrencyAmount operator -(CurrencyAmount left, CurrencyAmount right)
        {
            left.CheckCurrency(right.Currency, "-");
            return new CurrencyAmount(left.Amount - right.Amount, left.Currency);
        }
        public static CurrencyAmount operator -(CurrencyAmount right)
        {
            
            return new CurrencyAmount(- right.Amount, right.Currency);
        }

        void CheckCurrency(Currency type, string operation)
        {
            if (Currency == type) return;
            throw new InvalidOperationException(string.Format("Can't perform operation on different currencies: {0} {1} {2}", Currency, operation, type));
        }

        public static bool operator >(CurrencyAmount left, CurrencyAmount right)
        {
            left.CheckCurrency(right.Currency, ">");
            return left.Amount > right.Amount;
        }

        public override string ToString()
        {
            return string.Format("{0:0.##} {1}", Amount, Currency.ToString().ToUpper());
        }
    }



    [Serializable]
    public class CustomerCreated : IEvent
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public CustomerId Id { get; set; }
        public Currency Currency { get; set; }

        public override string ToString()
        {
            return string.Format("Customer {0} created with {1}", Name, Currency);
        }

    }
    [Serializable]
    public sealed class CreateCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string Name { get; set; }
        public Currency Currency { get; set; }

        public override string ToString()
        {
            return string.Format("Create {0} named '{1}' with {2}", Id, Name, Currency);
        }
    }
    [Serializable]
    public sealed class AddCustomerPayment : ICommand
    {
        public CustomerId Id { get; set; }
        public string Name { get; set; }
        public CurrencyAmount Amount { get; set; }

        public override string ToString()
        {
            return string.Format("Add {0} - '{1}'", Amount, Name);
        }
    }
    [Serializable]
    public sealed class ChargeCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string Name { get; set; }
        public CurrencyAmount Amount { get; set; }

        public override string ToString()
        {
            return string.Format("Charge {0} - '{1}'", Amount, Name);
        }
    }
    [Serializable]
    public sealed class CustomerPaymentAdded : IEvent
    {
        public CustomerId Id { get; set; }
        public string PaymentName { get; set; }
        public CurrencyAmount Payment { get; set; }
        public CurrencyAmount NewBalance { get; set; }
        public int Transaction { get; set; }
        public DateTime TimeUtc { get; set; }

        public override string ToString()
        {
            return string.Format("Added '{2}' {1} | Tx {0} => {3}", 
                Transaction, Payment, PaymentName, NewBalance);
        }
    }


    [Serializable]
    public sealed class CustomerChargeAdded : IEvent
    {
        public CustomerId Id { get; set; }
        public string ChargeName { get; set; }
        public CurrencyAmount Charge { get; set; }
        public CurrencyAmount NewBalance { get; set; }
        public int Transaction { get; set; }
        public DateTime TimeUtc { get; set; }

        public override string ToString()
        {
            return string.Format("Charged '{2}' {1} | Tx {0} => {3}",
                Transaction, Charge, ChargeName, NewBalance);
        }

    }
    [Serializable]
    public class RenameCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string NewName { get; set; }

        public override string ToString()
        {
            return string.Format("Rename {0} to '{1}'", Id, NewName);
        }
    }
    [Serializable]
    public class LockCustomerForAccountOverdraft : ICommand
    {
        public CustomerId Id { get; set; }
        public string Comment { get; set; }
    }
    [Serializable]
    public class LockCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string Reason { get; set; }
    }
    [Serializable]
    public class CustomerLocked : IEvent
    {
        public CustomerId Id { get; set; }
        public string Reason { get; set; }

        public override string ToString()
        {
            return string.Format("Customer locked: {0}", Reason);
        }
    }
    [Serializable]
    public class CustomerRenamed : IEvent
    {
        public string Name { get; set; }
        // normally you don't need old name. But here, 
        // we include it just for demo
        public string OldName { get; set; }
        public CustomerId Id { get; set; }
        public DateTime Renamed { get; set; }

        public override string ToString()
        {
            return string.Format("Customer renamed from '{0}' to '{1}'", OldName, Name);
        }
    }
}