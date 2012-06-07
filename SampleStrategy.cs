using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace lokad_iddd_sample
{
    public sealed class SampleStrategy : IEventStoreStrategy
    {
        public byte[] SerializeEvent(IEvent e)
        {
            using (var mem = new MemoryStream())
            {
                new BinaryFormatter().Serialize(mem, e);
                return mem.ToArray();
            }
        }

        public IEvent DeserializeEvent(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            {
                return (IEvent) new BinaryFormatter().Deserialize(mem);
            }
        }

        public string IdentityToString(IIdentity id)
        {
            return id.ToString();
        }
    }
}