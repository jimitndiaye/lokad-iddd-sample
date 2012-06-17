using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sample
{
    public interface IAppendOnlyStore : IDisposable
    {
        void Append(string name, byte[] data, int expectedVersion = -1);
        IEnumerable<DataWithVersion> ReadRecords(string name, int afterVersion, int maxCount);
        IEnumerable<DataWithName> ReadRecords(int afterVersion, int maxCount);

        void Close();
    }

    public sealed class DataWithVersion
    {
        public readonly int Version;
        public readonly byte[] Data;

        public DataWithVersion(int version, byte[] data)
        {
            Version = version;
            Data = data;
        }
    }
    public sealed class DataWithName
    {
        public readonly string Name;
        public readonly byte[] Data;

        public DataWithName(string name, byte[] data)
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