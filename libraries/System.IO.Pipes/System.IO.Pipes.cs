// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePipeHandle() : base (default(bool)) { }
        public SafePipeHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base (default(bool)) { }
        public override bool IsInvalid { get { throw new System.PlatformNotSupportedException(); } }
        protected override bool ReleaseHandle() { throw new System.PlatformNotSupportedException(); }
    }
}
namespace System.IO.Pipes
{
    public sealed partial class AnonymousPipeClientStream : System.IO.Pipes.PipeStream
    {
        public AnonymousPipeClientStream(System.IO.Pipes.PipeDirection direction, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeClientStream(System.IO.Pipes.PipeDirection direction, string pipeHandleAsString) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeClientStream(string pipeHandleAsString) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public override System.IO.Pipes.PipeTransmissionMode ReadMode { set { throw new System.PlatformNotSupportedException(); } }
        public override System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { throw new System.PlatformNotSupportedException(); } }
        ~AnonymousPipeClientStream() { }
    }
    public sealed partial class AnonymousPipeServerStream : System.IO.Pipes.PipeStream
    {
        public AnonymousPipeServerStream() : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, Microsoft.Win32.SafeHandles.SafePipeHandle serverSafePipeHandle, Microsoft.Win32.SafeHandles.SafePipeHandle clientSafePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability, int bufferSize) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public Microsoft.Win32.SafeHandles.SafePipeHandle ClientSafePipeHandle { get { throw new System.PlatformNotSupportedException(); } }
        public override System.IO.Pipes.PipeTransmissionMode ReadMode { set { throw new System.PlatformNotSupportedException(); } }
        public override System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { throw new System.PlatformNotSupportedException(); } }
        protected override void Dispose(bool disposing) { }
        public void DisposeLocalCopyOfClientHandle() { }
        ~AnonymousPipeServerStream() { }
        public string GetClientHandleAsString() { throw new System.PlatformNotSupportedException(); }
    }
    public sealed partial class NamedPipeClientStream : System.IO.Pipes.PipeStream
    {
        public NamedPipeClientStream(System.IO.Pipes.PipeDirection direction, bool isAsync, bool isConnected, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string pipeName) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeAccessRights desiredAccessRights, PipeOptions options, System.Security.Principal.TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options, System.Security.Principal.TokenImpersonationLevel impersonationLevel) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options, System.Security.Principal.TokenImpersonationLevel impersonationLevel, System.IO.HandleInheritability inheritability) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public int NumberOfServerInstances { get { throw new System.PlatformNotSupportedException(); } }
        protected internal override void CheckPipePropertyOperations() { }
        public void Connect() { }
        public void Connect(int timeout) { }
        public void Connect(System.TimeSpan timeout) { }
        public System.Threading.Tasks.Task ConnectAsync() { throw new System.PlatformNotSupportedException(); }
        public System.Threading.Tasks.Task ConnectAsync(int timeout) { throw new System.PlatformNotSupportedException(); }
        public System.Threading.Tasks.Task ConnectAsync(int timeout, System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public System.Threading.Tasks.Task ConnectAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public System.Threading.Tasks.Task ConnectAsync(System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        ~NamedPipeClientStream() { }
    }
    public sealed partial class NamedPipeServerStream : System.IO.Pipes.PipeStream
    {
        public const int MaxAllowedServerInstances = -1;
        public NamedPipeServerStream(System.IO.Pipes.PipeDirection direction, bool isAsync, bool isConnected, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options, int inBufferSize, int outBufferSize) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public System.IAsyncResult BeginWaitForConnection(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public void Disconnect() { }
        public void EndWaitForConnection(System.IAsyncResult asyncResult) { }
        ~NamedPipeServerStream() { }
        public string GetImpersonationUserName() { throw new System.PlatformNotSupportedException(); }
        public void RunAsClient(System.IO.Pipes.PipeStreamImpersonationWorker impersonationWorker) { }
        public void WaitForConnection() { }
        public System.Threading.Tasks.Task WaitForConnectionAsync() { throw new System.PlatformNotSupportedException(); }
        public System.Threading.Tasks.Task WaitForConnectionAsync(System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
    }
    [System.FlagsAttribute]
    public enum PipeAccessRights
    {
        ReadData = 1,
        WriteData = 2,
        CreateNewInstance = 4,
        ReadExtendedAttributes = 8,
        WriteExtendedAttributes = 16,
        ReadAttributes = 128,
        WriteAttributes = 256,
        Write = 274,
        Delete = 65536,
        ReadPermissions = 131072,
        Read = 131209,
        ReadWrite = 131483,
        ChangePermissions = 262144,
        TakeOwnership = 524288,
        Synchronize = 1048576,
        FullControl = 2032031,
        AccessSystemSecurity = 16777216,
    }
    public enum PipeDirection
    {
        In = 1,
        Out = 2,
        InOut = 3,
    }
    [System.FlagsAttribute]
    public enum PipeOptions
    {
        WriteThrough = -2147483648,
        None = 0,
        CurrentUserOnly = 536870912,
        Asynchronous = 1073741824,
        FirstPipeInstance = 524288
    }
    public abstract partial class PipeStream : System.IO.Stream
    {
        protected PipeStream(System.IO.Pipes.PipeDirection direction, int bufferSize) { }
        protected PipeStream(System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeTransmissionMode transmissionMode, int outBufferSize) { }
        public override bool CanRead { get { throw new System.PlatformNotSupportedException(); } }
        public override bool CanSeek { get { throw new System.PlatformNotSupportedException(); } }
        public override bool CanWrite { get { throw new System.PlatformNotSupportedException(); } }
        public virtual int InBufferSize { get { throw new System.PlatformNotSupportedException(); } }
        public bool IsAsync { get { throw new System.PlatformNotSupportedException(); } }
        public bool IsConnected { get { throw new System.PlatformNotSupportedException(); } protected set { throw new System.PlatformNotSupportedException(); } }
        protected bool IsHandleExposed { get { throw new System.PlatformNotSupportedException(); } }
        public bool IsMessageComplete { get { throw new System.PlatformNotSupportedException(); } }
        public override long Length { get { throw new System.PlatformNotSupportedException(); } }
        public virtual int OutBufferSize { get { throw new System.PlatformNotSupportedException(); } }
        public override long Position { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.IO.Pipes.PipeTransmissionMode ReadMode { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public Microsoft.Win32.SafeHandles.SafePipeHandle SafePipeHandle { get { throw new System.PlatformNotSupportedException(); } }
        public virtual System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { throw new System.PlatformNotSupportedException(); } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        protected internal virtual void CheckPipePropertyOperations() { }
        protected internal void CheckReadOperations() { }
        protected internal void CheckWriteOperations() { }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        protected void InitializeHandle(Microsoft.Win32.SafeHandles.SafePipeHandle? handle, bool isExposed, bool isAsync) { }
        public override int Read(byte[] buffer, int offset, int count) { throw new System.PlatformNotSupportedException(); }
        public override int Read(System.Span<byte> buffer) { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw new System.PlatformNotSupportedException(); }
        public override int ReadByte() { throw new System.PlatformNotSupportedException(); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw new System.PlatformNotSupportedException(); }
        public override void SetLength(long value) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public void WaitForPipeDrain() { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw new System.PlatformNotSupportedException(); }
        public override void WriteByte(byte value) { }
    }
    public delegate void PipeStreamImpersonationWorker();
    public enum PipeTransmissionMode
    {
        Byte = 0,
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        Message = 1,
    }
}
