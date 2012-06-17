#region (c) 2012-2012 Lokad - New BSD License 

// Copyright (c) Lokad 2012-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sample
{
    public interface IEventStore
    {
        EventStream LoadEventStream(IIdentity id);
        EventStream LoadEventStream(IIdentity id, int skipEvents, int maxCount);
        /// <summary>
        /// Appends events to server stream for the provided identity.
        /// </summary>
        /// <param name="id">identity to append to.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="events">The events to append.</param>
        /// <exception cref="OptimisticConcurrencyException">when new events were added to server
        /// since <paramref name="expectedVersion"/>
        /// </exception>
        void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events);
    }

    public class EventStream
    {
        // version of the event stream returned
        public int Version;
        // all events in the stream
        public List<IEvent> Events = new List<IEvent>();
    }

    /// <summary>
    /// Is thrown by event store if there were changes since our last version
    /// </summary>
    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {
        public int ActualVersion { get; private set; }
        public int ExpectedVersion { get; private set; }
        public IIdentity Id { get; private set; }
        public IList<IEvent> ActualEvents { get; private set; }

        OptimisticConcurrencyException(string message, int actualVersion, int expectedVersion, IIdentity id,
            IList<IEvent> serverEvents)
            : base(message)
        {
            ActualVersion = actualVersion;
            ExpectedVersion = expectedVersion;
            Id = id;
            ActualEvents = serverEvents;
        }

        public static OptimisticConcurrencyException Create(int actual, int expected, IIdentity id,
            IList<IEvent> serverEvents)
        {
            var message = string.Format("Expected v{0} but found v{1} in stream '{2}'", expected, actual, id);
            return new OptimisticConcurrencyException(message, actual, expected, id, serverEvents);
        }

        protected OptimisticConcurrencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {}
    }

    /// <summary>
    /// Is supposed to be thrown by the client code, when it fails to resolve concurrency problem
    /// </summary>
    [Serializable]
    public class RealConcurrencyException : Exception
    {
        public RealConcurrencyException() {}
        public RealConcurrencyException(string message) : base(message) {}
        public RealConcurrencyException(string message, Exception inner) : base(message, inner) {}

        protected RealConcurrencyException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}