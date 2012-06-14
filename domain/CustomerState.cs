using System.Collections.Generic;

namespace lokad_iddd_sample
{
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