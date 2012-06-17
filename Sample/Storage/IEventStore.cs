using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sample
{
    public interface IEventStore
    {
        EventStream LoadEventStream(IIdentity id, int skip = 0, int take = int.MaxValue);
        void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events);
    }

    public class EventStream
    {
        // version of the event stream returned
        public int Version;
        // all events in the stream
        public List<IEvent> Events = new List<IEvent>();
    }

    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {

        public int ActualVersion { get; private set; }
        public int ExpectedVersion { get; private set; }
        public IIdentity Id { get; private set; }
        public IList<IEvent> ServerEvents { get; private set; }

        OptimisticConcurrencyException(string message, int actualVersion, int expectedVersion, IIdentity id, IList<IEvent> serverEvents)
            : base(message)
        {
            ActualVersion = actualVersion;
            ExpectedVersion = expectedVersion;
            Id = id;
            ServerEvents = serverEvents;
        }

        public static OptimisticConcurrencyException Create(int actual, int expected, IIdentity id, IList<IEvent> serverEvents)
        {
            var message = string.Format("Expected v{0} but found v{1} in stream '{2}'", expected, actual, id);
            return new OptimisticConcurrencyException(message, actual, expected, id, serverEvents);
        }

        protected OptimisticConcurrencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}