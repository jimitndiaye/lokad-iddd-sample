using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Sample.Storage;

namespace Sample.StorageImplementations.Azure
{
    /// <summary>
    /// <para>This is embedded append-only store implemented on top of cloud page blobs 
    /// (for persisting data with one HTTP call).</para>
    /// <para>This store ensures that only one writer exists and writes to a given event store</para>
    /// </summary>
    public sealed class BlobAppendOnlyStore : IAppendOnlyStore
    {
        readonly CloudBlobContainer _container;

        // Caches
        readonly ConcurrentDictionary<string, DataWithVersion[]> _items = new ConcurrentDictionary<string, DataWithVersion[]>();
        DataWithName[] _all = new DataWithName[0];

        /// <summary>
        /// Used to synchronize access between multiple threads within one process
        /// </summary>
        readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();


        bool _closed;

        /// <summary>
        /// Currently open file
        /// </summary>
        AppendOnlyStream _currentWriter;

        /// <summary>
        /// Renewable Blob lease, used to prohibit multiple writers outside a given process
        /// </summary>
        AutoRenewLease _lock;

        public BlobAppendOnlyStore(CloudBlobContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            if (!_closed)
                Close();
        }

        public void InitializeWriter()
        {
            CreateIfNotExists(_container, TimeSpan.FromSeconds(60));
            // grab the ownership
            var blobReference = _container.GetBlobReference("lock");
            _lock = AutoRenewLease.GetOrThrow(blobReference);

            LoadCaches();
        }
        public void InitializeReader()
        {
            CreateIfNotExists(_container, TimeSpan.FromSeconds(60));
            LoadCaches();
        }

        public void Append(string streamName, byte[] data, long expectedStreamVersion = -1)
        {

            // should be locked
            try
            {
                _cacheLock.EnterWriteLock();

                var list = _items.GetOrAdd(streamName, s => new DataWithVersion[0]);
                if (expectedStreamVersion >= 0)
                {
                    if (list.Length != expectedStreamVersion)
                        throw new AppendOnlyStoreConcurrencyException(expectedStreamVersion, list.Length, streamName);
                }

                EnsureWriterExists(_all.Length);
                long commit = list.Length + 1;

                Persist(streamName, data, commit);
                AddToCaches(streamName, data, commit);
            }
            catch
            {
                Close();
                throw;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public IEnumerable<DataWithVersion> ReadRecords(string streamName, long afterVersion, int maxCount)
        {
            // no lock is needed, since we are polling immutable object.
            DataWithVersion[] list;
            return _items.TryGetValue(streamName, out list) ? list : Enumerable.Empty<DataWithVersion>();
        }

        public IEnumerable<DataWithName> ReadRecords(long afterVersion, int maxCount)
        {
            // collection is immutable so we don't care about locks
            return _all.Skip((int)afterVersion).Take(maxCount);
        }

        public void Close()
        {
            using (_lock)
            {
                _closed = true;

                if (_currentWriter == null)
                    return;

                var tmp = _currentWriter;
                _currentWriter = null;
                tmp.Dispose();
            }
        }

        IEnumerable<Record> EnumerateHistory()
        {
            // cleanup old pending files
            // load indexes
            // build and save missing indexes
            var datFiles = _container
                .ListBlobs()
                .OrderBy(s => s.Uri.ToString())
                .OfType<CloudPageBlob>();

            foreach (var fileInfo in datFiles)
            {
                using (var stream = new MemoryStream(fileInfo.DownloadByteArray()))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    Record result;
                    while (TryReadRecord(reader, out result))
                    {
                        yield return result;
                    }
                }
            }
        }

        static bool TryReadRecord(BinaryReader binary, out Record result)
        {
            result = null;

            try
            {
                var version = binary.ReadInt64();
                var name = binary.ReadString();
                var len = binary.ReadInt32();
                var bytes = binary.ReadBytes(len);

                var sha1 = binary.ReadBytes(20);
                if (sha1.All(s => s == 0))
                    throw new InvalidOperationException("definitely failed (zero hash)");

                byte[] actualSha1;
                PersistRecord(name, bytes, version, out actualSha1);

                if (!sha1.SequenceEqual(actualSha1))
                    throw new InvalidOperationException("hash mismatch");

                result = new Record(bytes, name, version);
                return true;
            }
            catch (EndOfStreamException)
            {
                // we are done
                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                // Auto-clean?
                return false;
            }
        }

        void LoadCaches()
        {
            try
            {
                _cacheLock.EnterWriteLock();

                foreach (var record in EnumerateHistory())
                {
                    AddToCaches(record.Name, record.Bytes, record.Version);
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        void AddToCaches(string key, byte[] buffer, long commit)
        {
            var record = new DataWithVersion(commit, buffer);
            _all = AddToNewArray(_all, new DataWithName(key, buffer));
            _items.AddOrUpdate(key, s => new[] { record }, (s, records) => AddToNewArray(records, record));
        }

        static T[] AddToNewArray<T>(T[] source, T item)
        {
            var copy = new T[source.Length + 1];
            Array.Copy(source, copy, source.Length);
            copy[source.Length] = item;
            return copy;
        }

        void Persist(string key, byte[] buffer, long commit)
        {
            byte[] hash;
            var bytes = PersistRecord(key, buffer, commit, out hash);

            if (!_currentWriter.Fits(bytes.Length + hash.Length))
            {
                CloseWriter();
                EnsureWriterExists(_all.Length);
            }

            _currentWriter.Write(bytes);
            _currentWriter.Write(hash);
            _currentWriter.Flush();
        }

        static byte[] PersistRecord(string key, byte[] buffer, long commit, out byte[] hash)
        {
            using (var sha1 = new SHA1Managed())
            using (var memory = new MemoryStream())
            {
                using (var crypto = new CryptoStream(memory, sha1, CryptoStreamMode.Write))
                using (var binary = new BinaryWriter(crypto, Encoding.UTF8))
                {
                    // version, ksz, vsz, key, value, sha1
                    binary.Write(commit);
                    binary.Write(key);
                    binary.Write(buffer.Length);
                    binary.Write(buffer);
                }

                hash = sha1.Hash;
                return memory.ToArray();
            }
        }

        void CloseWriter()
        {
            _currentWriter.Dispose();
            _currentWriter = null;
        }

        void EnsureWriterExists(long version)
        {


            if (_lock.Exception != null)
                throw new InvalidOperationException("Can not renew lease", _lock.Exception);

            if (_currentWriter != null)
                return;

            var fileName = string.Format("{0:00000000}-{1:yyyy-MM-dd-HHmm}.dat", version, DateTime.UtcNow);
            var blob = _container.GetPageBlobReference(fileName);
            blob.Create(1024 * 512);

            _currentWriter = new AppendOnlyStream(512, (i, bytes) => blob.WritePages(bytes, i), 1024 * 512);
        }

        static void CreateIfNotExists(CloudBlobContainer container, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    container.CreateIfNotExist();
                    return;
                }
                catch (StorageClientException e)
                {
                    // container is being deleted
                    if (!(e.ErrorCode == StorageErrorCode.ResourceAlreadyExists && e.StatusCode == HttpStatusCode.Conflict))
                        throw;
                }
                Thread.Sleep(500);
            }

            throw new TimeoutException(string.Format("Can not create container within {0} seconds.", timeout.TotalSeconds));
        }

        sealed class Record
        {
            public readonly byte[] Bytes;
            public readonly string Name;
            public readonly long Version;

            public Record(byte[] bytes, string name, long version)
            {
                Bytes = bytes;
                Name = name;
                Version = version;
            }
        }
    }
}