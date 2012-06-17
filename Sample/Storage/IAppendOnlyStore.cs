using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sample
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
}