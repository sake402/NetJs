// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Compression
{
    public sealed partial class BrotliCompressionOptions
    {
        public BrotliCompressionOptions() { }
        public int Quality { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
    }
    public partial struct BrotliDecoder : System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Buffers.OperationStatus Decompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten) { throw new System.PlatformNotSupportedException(); }
        public void Dispose() { }
        public static bool TryDecompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(); }
    }
    public partial struct BrotliEncoder : System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public BrotliEncoder(int quality, int window) { throw new System.PlatformNotSupportedException(); }
        public System.Buffers.OperationStatus Compress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) { throw new System.PlatformNotSupportedException(); }
        public void Dispose() { }
        public System.Buffers.OperationStatus Flush(System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(); }
        public static int GetMaxCompressedLength(int inputSize) { throw new System.PlatformNotSupportedException(); }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(); }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, int quality, int window) { throw new System.PlatformNotSupportedException(); }
    }
    public sealed partial class BrotliStream : System.IO.Stream
    {
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.BrotliCompressionOptions compressionOptions, bool leaveOpen = false) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public System.IO.Stream BaseStream { get { throw new System.PlatformNotSupportedException(); } }
        public override bool CanRead { get { throw new System.PlatformNotSupportedException(); } }
        public override bool CanSeek { get { throw new System.PlatformNotSupportedException(); } }
        public override bool CanWrite { get { throw new System.PlatformNotSupportedException(); } }
        public override long Length { get { throw new System.PlatformNotSupportedException(); } }
        public override long Position { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw new System.PlatformNotSupportedException(); }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw new System.PlatformNotSupportedException(); }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw new System.PlatformNotSupportedException(); }
        public override int EndRead(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) { throw new System.PlatformNotSupportedException(); }
        public override int Read(System.Span<byte> buffer) { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw new System.PlatformNotSupportedException(); }
        public override int ReadByte() { throw new System.PlatformNotSupportedException(); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw new System.PlatformNotSupportedException(); }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw new System.PlatformNotSupportedException(); }
        public override void WriteByte(byte value) { }
    }
}
