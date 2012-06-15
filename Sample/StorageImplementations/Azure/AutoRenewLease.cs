using System;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Sample.StorageImplementations.Azure
{
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