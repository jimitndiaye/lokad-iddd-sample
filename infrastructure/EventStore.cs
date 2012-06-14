using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace lokad_iddd_sample
{
    public sealed class EventStore : IEventStore
    {
        public EventStore(IAppendOnlyStore factory)
        {
            _factory = factory;
        }

        readonly IAppendOnlyStore _factory;

        readonly BinaryFormatter _formatter = new BinaryFormatter();
        byte[] SerializeEvent(IEvent[] e)
        {
            using (var mem = new MemoryStream())
            {
                _formatter.Serialize(mem, e);
                return mem.ToArray();
            }
        }

        IEvent[] DeserializeEvent(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            {
                return (IEvent[])_formatter.Deserialize(mem);
            }
        }

        string IdentityToString(IIdentity id)
        {
            return id.ToString();
        }

        public EventStream LoadEventStream(IIdentity id, int skip, int take)
        {
            var name = IdentityToString(id);
            var records = _factory.ReadRecords(name, skip, take).ToList();
            var stream = new EventStream();

            foreach (var tapeRecord in records)
            {
                stream.Events.AddRange(DeserializeEvent(tapeRecord.Data));
                stream.Version = tapeRecord.Version;
            }
            return stream;
        }

        public void AppendToStream(IIdentity id, int originalVersion, ICollection<IEvent> events)
        {
            if (events.Count == 0)
                return;
            var name = IdentityToString(id);
            var data = SerializeEvent(events.ToArray());
            _factory.Append(name, data, originalVersion);

            // technically there should be parallel process that gets published changes from
            // event store and sends them via messages.
            // however, for simplicity, we'll just send them to console from here

            foreach (var @event in events)
            {
                Console.WriteLine("  {0}@{1} => {2}", id,originalVersion, @event);
            }
        }
    }
}