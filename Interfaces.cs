#region (c) 2012-2012 Lokad - New BSD License 

// Copyright (c) Lokad 2012-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace lokad_iddd_sample
{

    public interface IAppendOnlyStore : IDisposable
    {
        void Append(string key, byte[] buffer, int serverVersion = -1);
        IEnumerable<TapeRecord> ReadRecords(string key, int afterVersion, int maxCount);
        IEnumerable<AppendRecord> ReadRecords(int afterVersion, int maxCount);

        void Close();
    }

    public sealed class TapeRecord
    {
        public readonly int Version;
        public readonly byte[] Data;

        public TapeRecord(int version, byte[] data)
        {
            Version = version;
            Data = data;
        }
    }
    public sealed class AppendRecord
    {
        public readonly string Name;
        public readonly byte[] Data;

        public AppendRecord(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }
    }

    /// <summary>
    /// Is thrown internally, when storage version does not match the condition specified in <see cref="TapeAppendCondition"/>
    /// </summary>
    [Serializable]
    public class AppendOnlyStoreConcurrencyException : Exception
    {
        
        public int ExpectedVersion { get; private set; }
        public int ActualVersion { get; private set; }
        public string Name { get; private set; }

        protected AppendOnlyStoreConcurrencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }

        public AppendOnlyStoreConcurrencyException(int expectedVersion, int actualVersion, string name)
            : base(
                string.Format("Expected version {0} in stream '{1}' but got {2}", expectedVersion, name, actualVersion))
        {
            Name = name;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }



    public interface IEventStore
    {
        EventStream LoadEventStream(IIdentity id, int skip, int take);
        void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events);
    }

    public class EventStream
    {
        // version of the event stream returned
        public int Version;
        // all events in the stream
        public List<IEvent> Events = new List<IEvent>();
    }

    public interface IEvent {}

    public interface ICommand {}

    public interface IIdentity {}

    public interface IPricingModel
    {
        CurrencyAmount GetOverdraftThreshold(Currency currency);
    }

    [Serializable]
    public class EventStoreConcurrencyException : Exception
    {

        public int ActualVersion { get; private set; }
        public int ExpectedVersion { get; private set; }
        public string EventStreamName { get; private set; }


        public EventStoreConcurrencyException(string message, int actualVersion, int expectedVersion, string eventStreamName) : base(message)
        {
            ActualVersion = actualVersion;
            ExpectedVersion = expectedVersion;
            EventStreamName = eventStreamName;
        }

        public static EventStoreConcurrencyException Create(int actual, int expected, string name)
        {
            var message = string.Format("Expected v{0} but found v{1} in stream '{2}'", expected, actual, name);
            return new EventStoreConcurrencyException(message,actual, expected, name);
        }

        protected EventStoreConcurrencyException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}