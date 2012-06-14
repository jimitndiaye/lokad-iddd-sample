using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace lokad_iddd_sample
{
    public sealed class EventStore : IEventStore
    {
        public EventStore(IAppendOnlyStore factory, IEventStoreStrategy strategy)
        {
            _factory = factory;
            _strategy = strategy;
        }

        readonly IAppendOnlyStore _factory;
        readonly IEventStoreStrategy _strategy;

        public EventStream LoadEventStream(IIdentity id, int skip, int take)
        {
            var name = _strategy.IdentityToString(id);
            var records = _factory.ReadRecords(name, skip, take).ToList();
            var stream = new EventStream();

            foreach (var tapeRecord in records)
            {
                stream.Events.AddRange(_strategy.DeserializeEvent(tapeRecord.Data));
                stream.Version = tapeRecord.Version;
            }
            return stream;
        }

        public void AppendToStream(IIdentity id, int originalVersion, ICollection<IEvent> events)
        {
            if (events.Count == 0)
                return;
            var name = _strategy.IdentityToString(id);
            var data = _strategy.SerializeEvent(events.ToArray());
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