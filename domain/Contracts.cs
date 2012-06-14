using System;

namespace lokad_iddd_sample
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



    [Serializable]
    public class CustomerCreated : IEvent
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public CustomerId Id { get; set; }

        public override string ToString()
        {
            return string.Format("Customer {0} created", Name);
        }

    }
    [Serializable]
    public sealed class CreateCustomer : ICommand
    {
        public CustomerId Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("Create {0} named '{1}'", Id, Name);
        }
    }

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