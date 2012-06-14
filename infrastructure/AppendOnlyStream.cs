using System;
using System.IO;

namespace lokad_iddd_sample
{
    /// <summary>
    /// Helps to write data to the underlying store, which accepts only
    /// pages with specific size
    /// </summary>
    public sealed class AppendOnlyStream : IDisposable
    {
        readonly int _pageSizeInBytes;
        readonly AppendWriterDelegate _writer;
        readonly int _maxByteCount;
        MemoryStream _pending;

        int _bytesWritten;
        int _fullPagesFlushed;

        public AppendOnlyStream(int pageSizeInBytes, AppendWriterDelegate writer, int maxByteCount)
        {
            _writer = writer;
            _maxByteCount = maxByteCount;
            _pageSizeInBytes = pageSizeInBytes;
            _pending = new MemoryStream();
        }

        public bool Fits(int byteCount)
        {
            return (_bytesWritten + byteCount <= _maxByteCount);
        }

        public void Write(byte[] buffer)
        {
            _pending.Write(buffer, 0, buffer.Length);
            _bytesWritten += buffer.Length;
        }

        public void Flush()
        {
            if (_pending.Length == 0)
                return;

            var size = (int)_pending.Length;
            var padSize = (_pageSizeInBytes - size % _pageSizeInBytes) % _pageSizeInBytes;

            using (var stream = new MemoryStream(size + padSize))
            {
                stream.Write(_pending.ToArray(), 0, (int)_pending.Length);
                if (padSize > 0)
                    stream.Write(new byte[padSize], 0, padSize);

                stream.Position = 0;
                _writer(_fullPagesFlushed * _pageSizeInBytes, stream);
            }

            var fullPagesFlushed = size / _pageSizeInBytes;

            if (fullPagesFlushed <= 0)
                return;

            // Copy remainder to the new stream and dispose the old stream
            var newStream = new MemoryStream();
            _pending.Position = fullPagesFlushed * _pageSizeInBytes;
            _pending.CopyTo(newStream);
            _pending.Dispose();
            _pending = newStream;

            _fullPagesFlushed += fullPagesFlushed;
        }

        public void Dispose()
        {
            _pending.Dispose();
        }
    }

    /// <summary>
    /// Delegate that writes pages to the underlying paged store.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="source">The source.</param>
    public delegate void AppendWriterDelegate(int offset, Stream source);
}