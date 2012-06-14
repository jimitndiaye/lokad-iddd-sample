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
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace lokad_iddd_sample
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
        readonly ConcurrentDictionary<string, TapeRecord[]> _items = new ConcurrentDictionary<string, TapeRecord[]>();
        AppendRecord[] _all = new AppendRecord[0];

        /// <summary>
        /// Used to synchronize access between multiple threads within one process
        /// </summary>
        readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();


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

        public void Append(string key, byte[] buffer, int serverVersion = -1)
        {
            // should be locked
            try
            {
                _rwLock.EnterWriteLock();

                var list = _items.GetOrAdd(key, s => new TapeRecord[0]);
                if (serverVersion >= 0)
                {
                    if (list.Length != serverVersion)
                        throw new AppendOnlyStoreConcurrencyException(serverVersion, list.Length, key);
                }

                EnsureWriterExists(_all.Length);
                int commit = list.Length + 1;

                Persist(key, buffer, commit);
                AddToCaches(key, buffer, commit);
            }
            catch
            {
                Close();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public IEnumerable<TapeRecord> ReadRecords(string key, int afterVersion, int maxCount)
        {
            // no lock is needed, since we are polling immutable object.
            TapeRecord[] list;
            return _items.TryGetValue(key, out list) ? list : Enumerable.Empty<TapeRecord>();
        }

        public IEnumerable<AppendRecord> ReadRecords(int afterVersion, int maxCount)
        {
            // collection is immutable so we don't care about locks
            return _all.Skip((int)afterVersion).Take(maxCount);
        }

        public void Close()
        {
            using (_lock)
            {
                _closed = true;
                _currentWriter = null;
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
                var version = binary.ReadInt32();
                var name = binary.ReadString();
                var len = binary.ReadInt32();
                var bytes = binary.ReadBytes(len);
                var sha = binary.ReadBytes(20); // SHA1. TODO: verify data
                if (sha.All(s => s == 0))
                    throw new InvalidOperationException("definitely failed");

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
                _rwLock.EnterWriteLock();

                foreach (var record in EnumerateHistory())
                {
                    AddToCaches(record.Name, record.Bytes, record.Version);
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        void AddToCaches(string key, byte[] buffer, int commit)
        {
            var record = new TapeRecord(commit, buffer);
            _all = AddToNewArray(_all, new AppendRecord(key, buffer));
            _items.AddOrUpdate(key, s => new[] { record }, (s, records) => AddToNewArray(records, record));
        }

        static T[] AddToNewArray<T>(T[] source, T item)
        {
            var copy = new T[source.Length + 1];
            Array.Copy(source, copy, source.Length);
            copy[source.Length] = item;
            return copy;
        }

        void Persist(string key, byte[] buffer, int commit)
        {
            using (var sha1 = new SHA1Managed())
            {
                byte[] bytes;
                // version, ksz, vsz, key, value, sha1
                using (var memory = new MemoryStream())
                {
                    using (var crypto = new CryptoStream(memory, sha1, CryptoStreamMode.Write))
                    using (var binary = new BinaryWriter(crypto, Encoding.UTF8))
                    {
                        binary.Write(commit);
                        binary.Write(key);
                        binary.Write(buffer.Length);
                        binary.Write(buffer);
                    }
                    bytes = memory.ToArray();
                }

                if (!_currentWriter.Fits(bytes.Length + sha1.Hash.Length))
                {
                    CloseWriter();
                    EnsureWriterExists(_all.Length);
                }

                _currentWriter.Write(bytes);
                _currentWriter.Write(sha1.Hash);
                _currentWriter.Flush();
            }
        }

        public void Initialize()
        {
            _container.CreateIfNotExist();

            // grab the ownership
            _lock = AutoRenewLease.GetOrThrow(_container.GetBlobReference("lock"));
            LoadCaches();
        }

        void CloseWriter()
        {
            _currentWriter.Dispose();
            _currentWriter = null;
        }

        void EnsureWriterExists(long version)
        {
            if (_currentWriter != null)
                return;

            var fileName = string.Format("{0:00000000}-{1:yyyy-MM-dd-HHmm}.dat", version, DateTime.UtcNow);
            var blob = _container.GetPageBlobReference(fileName);
            blob.Create(1024 * 512);

            _currentWriter = new AppendOnlyStream(512, (i, bytes) => blob.WritePages(bytes, i), 1024 * 512);
        }

        sealed class Record
        {
            public readonly byte[] Bytes;
            public readonly string Name;
            public readonly int Version;

            public Record(byte[] bytes, string name, int version)
            {
                Bytes = bytes;
                Name = name;
                Version = version;
            }
        }
    }

    public class AutoRenewLease : IDisposable
    {
        readonly CloudBlob _blob;
        readonly string _leaseId;
        bool _disposed;
        Thread _renewalThread;

        public static AutoRenewLease GetOrThrow(CloudBlob blob)
        {
            blob.Container.CreateIfNotExist();

            try
            {
                var requestOptions = new BlobRequestOptions
                {
                    AccessCondition = AccessCondition.IfNoneMatch("*")
                };
                blob.UploadByteArray(new byte[0], requestOptions);
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode != StorageErrorCode.BlobAlreadyExists
                    && e.StatusCode != HttpStatusCode.PreconditionFailed)
                // 412 from trying to modify a blob that's leased
                {
                    throw;
                }
            }

            var leaseId = AcquireLease(blob);
            if (string.IsNullOrEmpty(leaseId))
                throw new InvalidOperationException("Failed to get lease");

            return new AutoRenewLease(blob, leaseId);
        }

        AutoRenewLease(CloudBlob blob, string leaseId)
        {
            _blob = blob;
            _leaseId = leaseId;


            _renewalThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(40));
                    RenewLease(blob, _leaseId);
                }
            });
            _renewalThread.Start();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_renewalThread != null)
            {
                _renewalThread.Abort();
                ReleaseLease(_blob, _leaseId);
                _renewalThread = null;
            }
            _disposed = true;
        }



        static string AcquireLease(CloudBlob blob)
        {
            var creds = blob.ServiceClient.Credentials;
            var transformedUri = new Uri(creds.TransformUri(blob.Uri.AbsoluteUri));
            var req = BlobRequest.Lease(transformedUri,
                90, // timeout (in seconds)
                LeaseAction.Acquire, // as opposed to "break" "release" or "renew"
                null); // name of the existing lease, if any
            blob.ServiceClient.Credentials.SignRequest(req);
            using (var response = req.GetResponse())
            {
                return response.Headers["x-ms-lease-id"];
            }
        }

        static void DoLeaseOperation(CloudBlob blob, string leaseId, LeaseAction action)
        {
            var creds = blob.ServiceClient.Credentials;
            var transformedUri = new Uri(creds.TransformUri(blob.Uri.AbsoluteUri));
            var req = BlobRequest.Lease(transformedUri, 90, action, leaseId);
            creds.SignRequest(req);
            using (req.GetResponse())
            {
            }
        }

        static void ReleaseLease(CloudBlob blob, string leaseId)
        {
            DoLeaseOperation(blob, leaseId, LeaseAction.Release);
        }

        static void RenewLease(CloudBlob blob, string leaseId)
        {
            DoLeaseOperation(blob, leaseId, LeaseAction.Renew);
        }
    }

}