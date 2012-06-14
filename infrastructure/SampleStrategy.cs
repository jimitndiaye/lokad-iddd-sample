using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace lokad_iddd_sample
{
    public sealed class SampleStrategy : IEventStoreStrategy
    {
        readonly BinaryFormatter _formatter = new BinaryFormatter();
        public byte[] SerializeEvent(IEvent e)
        {
            using (var mem = new MemoryStream())
            {
                _formatter.Serialize(mem, e);
                return mem.ToArray();
            }
        }

        public IEvent DeserializeEvent(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            {
                return (IEvent) _formatter.Deserialize(mem);
            }
        }

        public string IdentityToString(IIdentity id)
        {
            return id.ToString();
        }
    }
}