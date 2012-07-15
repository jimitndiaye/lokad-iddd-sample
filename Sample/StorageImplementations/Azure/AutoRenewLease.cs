using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Sample.StorageImplementations.Azure
{
    /// <summary>
    /// Helper class that keeps renewing ownership lease (lock) of a specific blob,
    /// while the process is alive
    /// </summary>
    public class AutoRenewLease : IDisposable
    {
        readonly CloudBlob _blob;
        readonly string _leaseId;
        bool _disposed;
        Thread _renewalThread;
        readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();

        AutoRenewLease(CloudBlob blob, string leaseId)
        {
            _blob = blob;
            _leaseId = leaseId;

            _renewalThread = new Thread(() =>
            {
                var token = _cancelSource.Token;
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(40));
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var e = DoUntilTrue(Try4Times, CancellationToken.None,
                            () => RenewLease(blob, _leaseId) || token.IsCancellationRequested);

                        if (e != null)
                        {
                            Exception = e;
                            break;
                        }

                        var sw = Stopwatch.StartNew();
                        while (sw.Elapsed.TotalSeconds < 40 && !token.IsCancellationRequested)
                        {
                            token.WaitHandle.WaitOne(100);
                        }
                    }
                    catch (Exception e)
                    {
                        Exception = e;
                        break;
                    }
                }
            });
            _renewalThread.Start();
        }

        public Exception Exception { get; private set; }

        public static AutoRenewLease GetOrThrow(CloudBlob blob)
        {
            blob.Container.CreateIfNotExist();

            // Create lock blob
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

            string leaseId = null;
            var ex = DoUntilTrue(Try4Times, CancellationToken.None, () =>
            {
                leaseId = AcquireLease(blob);
                return !String.IsNullOrEmpty(leaseId);
            });
            if (ex != null)
                throw new InvalidOperationException("Failed to get lease", ex);

            // Either we get lease or throw timeout exception
            if (String.IsNullOrEmpty(leaseId))
                throw new InvalidOperationException();

            return new AutoRenewLease(blob, leaseId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_renewalThread != null)
            {
                _cancelSource.Cancel();

                DoUntilTrue(Try4Times, CancellationToken.None,
                    () => ReleaseLease(_blob, _leaseId));
                _renewalThread = null;
            }
            _disposed = true;
        }

        static string AcquireLease(CloudBlob blob)
        {
            var creds = blob.ServiceClient.Credentials;
            var transformedUri = new Uri(creds.TransformUri(blob.Uri.AbsoluteUri));
            var req = BlobRequest.Lease(transformedUri, 10, LeaseAction.Acquire, null);
            req.Headers.Add("x-ms-lease-duration", "60");
            creds.SignRequest(req);

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                switch (statusCode)
                {
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        return null;
                    default:
                        throw;
                }
            }

            try
            {
                return response.StatusCode == HttpStatusCode.Created
                    ? response.Headers["x-ms-lease-id"]
                    : null;
            }
            finally
            {
                response.Close();
            }
        }

        static bool DoLeaseOperation(CloudBlob blob, string leaseId, LeaseAction action)
        {
            var creds = blob.ServiceClient.Credentials;
            var transformedUri = new Uri(creds.TransformUri(blob.Uri.ToString()));
            var req = BlobRequest.Lease(transformedUri, 10, action, leaseId);
            creds.SignRequest(req);

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                switch (statusCode)
                {
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        return false;
                    default:
                        throw;
                }
            }

            try
            {
                var expectedCode = action == LeaseAction.Break ? HttpStatusCode.Accepted : HttpStatusCode.OK;
                return response.StatusCode == expectedCode;
            }
            finally
            {
                response.Close();
            }
        }

        static bool ReleaseLease(CloudBlob blob, string leaseId)
        {
            return DoLeaseOperation(blob, leaseId, LeaseAction.Release);
        }

        static bool RenewLease(CloudBlob blob, string leaseId)
        {
            return DoLeaseOperation(blob, leaseId, LeaseAction.Renew);
        }

        /// <remarks>Policy must support exceptions being null.</remarks>
        static Exception DoUntilTrue(ShouldRetry retryPolicy, CancellationToken token, Func<bool> action)
        {
            var retryCount = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (action())
                    {
                        return null;
                    }

                    TimeSpan delay;
                    if (retryPolicy(retryCount, null, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            token.WaitHandle.WaitOne(delay);
                        }

                        continue;
                    }

                    return new TimeoutException("Failed to reach a successful result in a limited number of retrials");
                }
                catch (Exception e)
                {
                    TimeSpan delay;
                    if (retryPolicy(retryCount, e, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            token.WaitHandle.WaitOne(delay);
                        }

                        continue;
                    }

                    return e;
                }
            }
        }

        /// <summary>
        /// Retry policy for optimistic concurrency retrials.
        /// </summary>
        static readonly ShouldRetry Try4Times = delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (currentRetryCount >= 5)
            {
                retryInterval = TimeSpan.Zero;
                return false;
            }

            retryInterval = TimeSpan.FromSeconds(2);

            return true;
        };
    }

}