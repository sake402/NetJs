// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    [System.ObsoleteAttribute("AuthenticationManager is not supported. Methods will no-op or throw PlatformNotSupportedException.", DiagnosticId="SYSLIB0009", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public partial class AuthenticationManager
    {
        internal AuthenticationManager() { }
        public static System.Net.ICredentialPolicy? CredentialPolicy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Collections.Specialized.StringDictionary CustomTargetNameDictionary { get { throw new System.PlatformNotSupportedException(); } }
        public static System.Collections.IEnumerator RegisteredModules { get { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.Authorization? Authenticate(string challenge, System.Net.WebRequest request, System.Net.ICredentials credentials) { throw new System.PlatformNotSupportedException(); }
        public static System.Net.Authorization? PreAuthenticate(System.Net.WebRequest request, System.Net.ICredentials credentials) { throw new System.PlatformNotSupportedException(); }
        public static void Register(System.Net.IAuthenticationModule authenticationModule) { }
        public static void Unregister(System.Net.IAuthenticationModule authenticationModule) { }
        public static void Unregister(string authenticationScheme) { }
    }
    public partial class Authorization
    {
        public Authorization(string? token) { }
        public Authorization(string? token, bool finished) { }
        public Authorization(string? token, bool finished, string? connectionGroupId) { }
        public bool Complete { get { throw new System.PlatformNotSupportedException(); } }
        public string? ConnectionGroupId { get { throw new System.PlatformNotSupportedException(); } }
        public string? Message { get { throw new System.PlatformNotSupportedException(); } }
        public bool MutuallyAuthenticated { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string[]? ProtectionRealm { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
    }
    public partial class FileWebRequest : System.Net.WebRequest, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("Serialization has been deprecated for FileWebRequest.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected FileWebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override string? ConnectionGroupName { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override long ContentLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string? ContentType { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.ICredentials? Credentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } }
        public override string Method { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override bool PreAuthenticate { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.IWebProxy? Proxy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Uri RequestUri { get { throw new System.PlatformNotSupportedException(); } }
        public override int Timeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override bool UseDefaultCredentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for FileWebRequest.")]
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetRequestStream() { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { throw new System.PlatformNotSupportedException(); }
        public override System.Net.WebResponse GetResponse() { throw new System.PlatformNotSupportedException(); }
        public override System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for FileWebRequest.")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class FileWebResponse : System.Net.WebResponse, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("Serialization has been deprecated for FileWebResponse.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected FileWebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override long ContentLength { get { throw new System.PlatformNotSupportedException(); } }
        public override string ContentType { get { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } }
        public override System.Uri ResponseUri { get { throw new System.PlatformNotSupportedException(); } }
        public override bool SupportsHeaders { get { throw new System.PlatformNotSupportedException(); } }
        public override void Close() { }
        [System.ObsoleteAttribute("Serialization has been deprecated for FileWebResponse.")]
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetResponseStream() { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for FileWebResponse.")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public enum FtpStatusCode
    {
        Undefined = 0,
        RestartMarker = 110,
        ServiceTemporarilyNotAvailable = 120,
        DataAlreadyOpen = 125,
        OpeningData = 150,
        CommandOK = 200,
        CommandExtraneous = 202,
        DirectoryStatus = 212,
        FileStatus = 213,
        SystemType = 215,
        SendUserCommand = 220,
        ClosingControl = 221,
        ClosingData = 226,
        EnteringPassive = 227,
        LoggedInProceed = 230,
        ServerWantsSecureSession = 234,
        FileActionOK = 250,
        PathnameCreated = 257,
        SendPasswordCommand = 331,
        NeedLoginAccount = 332,
        FileCommandPending = 350,
        ServiceNotAvailable = 421,
        CantOpenData = 425,
        ConnectionClosed = 426,
        ActionNotTakenFileUnavailableOrBusy = 450,
        ActionAbortedLocalProcessingError = 451,
        ActionNotTakenInsufficientSpace = 452,
        CommandSyntaxError = 500,
        ArgumentSyntaxError = 501,
        CommandNotImplemented = 502,
        BadCommandSequence = 503,
        NotLoggedIn = 530,
        AccountNeeded = 532,
        ActionNotTakenFileUnavailable = 550,
        ActionAbortedUnknownPageType = 551,
        FileActionAborted = 552,
        ActionNotTakenFilenameNotAllowed = 553,
    }
    public sealed partial class FtpWebRequest : System.Net.WebRequest
    {
        internal FtpWebRequest() { }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string? ConnectionGroupName { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override long ContentLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public long ContentOffset { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string? ContentType { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public override System.Net.ICredentials? Credentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static new System.Net.Cache.RequestCachePolicy? DefaultCachePolicy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool EnableSsl { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool KeepAlive { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string Method { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override bool PreAuthenticate { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.IWebProxy? Proxy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int ReadWriteTimeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public string? RenameTo { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Uri RequestUri { get { throw new System.PlatformNotSupportedException(); } }
        public System.Net.ServicePoint ServicePoint { get { throw new System.PlatformNotSupportedException(); } }
        public override int Timeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool UseBinary { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override bool UseDefaultCredentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool UsePassive { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public override System.IO.Stream GetRequestStream() { throw new System.PlatformNotSupportedException(); }
        public override System.Net.WebResponse GetResponse() { throw new System.PlatformNotSupportedException(); }
    }
    public partial class FtpWebResponse : System.Net.WebResponse, System.IDisposable
    {
        internal FtpWebResponse() { }
        public string? BannerMessage { get { throw new System.PlatformNotSupportedException(); } }
        public override long ContentLength { get { throw new System.PlatformNotSupportedException(); } }
        public string? ExitMessage { get { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } }
        public System.DateTime LastModified { get { throw new System.PlatformNotSupportedException(); } }
        public override System.Uri ResponseUri { get { throw new System.PlatformNotSupportedException(); } }
        public System.Net.FtpStatusCode StatusCode { get { throw new System.PlatformNotSupportedException(); } }
        public string? StatusDescription { get { throw new System.PlatformNotSupportedException(); } }
        public override bool SupportsHeaders { get { throw new System.PlatformNotSupportedException(); } }
        public string? WelcomeMessage { get { throw new System.PlatformNotSupportedException(); } }
        public override void Close() { }
        public override System.IO.Stream GetResponseStream() { throw new System.PlatformNotSupportedException(); }
    }
    [System.ObsoleteAttribute("GlobalProxySelection has been deprecated. Use WebRequest.DefaultWebProxy instead to access and set the global default proxy. Use 'null' instead of GetEmptyWebProxy.")]
    public partial class GlobalProxySelection
    {
        public GlobalProxySelection() { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public static System.Net.IWebProxy Select { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.IWebProxy GetEmptyWebProxy() { throw new System.PlatformNotSupportedException(); }
    }
    public delegate void HttpContinueDelegate(int StatusCode, System.Net.WebHeaderCollection httpHeaders);
    public partial class HttpWebRequest : System.Net.WebRequest, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected HttpWebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public string? Accept { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Uri Address { get { throw new System.PlatformNotSupportedException(); } }
        public virtual bool AllowAutoRedirect { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual bool AllowReadStreamBuffering { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual bool AllowWriteStreamBuffering { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Net.DecompressionMethods AutomaticDecompression { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string? Connection { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string? ConnectionGroupName { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override long ContentLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string? ContentType { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Net.HttpContinueDelegate? ContinueDelegate { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int ContinueTimeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.CookieContainer? CookieContainer { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.ICredentials? Credentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.DateTime Date { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static new System.Net.Cache.RequestCachePolicy? DefaultCachePolicy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static int DefaultMaximumErrorResponseLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static int DefaultMaximumResponseHeadersLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string? Expect { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual bool HaveResponse { get { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string Host { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.DateTime IfModifiedSince { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool KeepAlive { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int MaximumAutomaticRedirections { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int MaximumResponseHeadersLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string? MediaType { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override string Method { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool Pipelined { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override bool PreAuthenticate { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Version ProtocolVersion { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.IWebProxy? Proxy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int ReadWriteTimeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string? Referer { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Uri RequestUri { get { throw new System.PlatformNotSupportedException(); } }
        public bool SendChunked { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Net.Security.RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Net.ServicePoint ServicePoint { get { throw new System.PlatformNotSupportedException(); } }
        public virtual bool SupportsCookieContainer { get { throw new System.PlatformNotSupportedException(); } }
        public override int Timeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string? TransferEncoding { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool UnsafeAuthenticatedConnectionSharing { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override bool UseDefaultCredentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string? UserAgent { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override void Abort() { }
        public void AddRange(int range) { }
        public void AddRange(int from, int to) { }
        public void AddRange(long range) { }
        public void AddRange(long from, long to) { }
        public void AddRange(string rangeSpecifier, int range) { }
        public void AddRange(string rangeSpecifier, int from, int to) { }
        public void AddRange(string rangeSpecifier, long range) { }
        public void AddRange(string rangeSpecifier, long from, long to) { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult, out System.Net.TransportContext? context) { throw new System.PlatformNotSupportedException(); }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for HttpWebRequest.")]
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetRequestStream() { throw new System.PlatformNotSupportedException(); }
        public System.IO.Stream GetRequestStream(out System.Net.TransportContext? context) { throw new System.PlatformNotSupportedException(); }
        public override System.Net.WebResponse GetResponse() { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for HttpWebRequest.")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class HttpWebResponse : System.Net.WebResponse, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("This API supports the .NET infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public HttpWebResponse() { }
        [System.ObsoleteAttribute("Serialization has been deprecated for HttpWebResponse.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected HttpWebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public string? CharacterSet { get { throw new System.PlatformNotSupportedException(); } }
        public string ContentEncoding { get { throw new System.PlatformNotSupportedException(); } }
        public override long ContentLength { get { throw new System.PlatformNotSupportedException(); } }
        public override string ContentType { get { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.CookieCollection Cookies { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public override System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } }
        public override bool IsMutuallyAuthenticated { get { throw new System.PlatformNotSupportedException(); } }
        public System.DateTime LastModified { get { throw new System.PlatformNotSupportedException(); } }
        public virtual string Method { get { throw new System.PlatformNotSupportedException(); } }
        public System.Version ProtocolVersion { get { throw new System.PlatformNotSupportedException(); } }
        public override System.Uri ResponseUri { get { throw new System.PlatformNotSupportedException(); } }
        public string Server { get { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.HttpStatusCode StatusCode { get { throw new System.PlatformNotSupportedException(); } }
        public virtual string StatusDescription { get { throw new System.PlatformNotSupportedException(); } }
        public override bool SupportsHeaders { get { throw new System.PlatformNotSupportedException(); } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        [System.ObsoleteAttribute("Serialization has been deprecated for HttpWebResponse.")]
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public string GetResponseHeader(string headerName) { throw new System.PlatformNotSupportedException(); }
        public override System.IO.Stream GetResponseStream() { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for HttpWebResponse.")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial interface IAuthenticationModule
    {
        string AuthenticationType { get; }
        bool CanPreAuthenticate { get; }
        System.Net.Authorization? Authenticate(string challenge, System.Net.WebRequest request, System.Net.ICredentials credentials);
        System.Net.Authorization? PreAuthenticate(System.Net.WebRequest request, System.Net.ICredentials credentials);
    }
    public partial interface ICredentialPolicy
    {
        bool ShouldSendCredential(System.Uri challengeUri, System.Net.WebRequest request, System.Net.NetworkCredential credential, System.Net.IAuthenticationModule authenticationModule);
    }
    public partial interface IWebRequestCreate
    {
        System.Net.WebRequest Create(System.Uri uri);
    }
    public partial class ProtocolViolationException : System.InvalidOperationException, System.Runtime.Serialization.ISerializable
    {
        public ProtocolViolationException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected ProtocolViolationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public ProtocolViolationException(string? message) { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class WebException : System.InvalidOperationException, System.Runtime.Serialization.ISerializable
    {
        public WebException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected WebException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public WebException(string? message) { }
        public WebException(string? message, System.Exception? innerException) { }
        public WebException(string? message, System.Exception? innerException, System.Net.WebExceptionStatus status, System.Net.WebResponse? response) { }
        public WebException(string? message, System.Net.WebExceptionStatus status) { }
        public System.Net.WebResponse? Response { get { throw new System.PlatformNotSupportedException(); } }
        public System.Net.WebExceptionStatus Status { get { throw new System.PlatformNotSupportedException(); } }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public enum WebExceptionStatus
    {
        Success = 0,
        NameResolutionFailure = 1,
        ConnectFailure = 2,
        ReceiveFailure = 3,
        SendFailure = 4,
        PipelineFailure = 5,
        RequestCanceled = 6,
        ProtocolError = 7,
        ConnectionClosed = 8,
        TrustFailure = 9,
        SecureChannelFailure = 10,
        ServerProtocolViolation = 11,
        KeepAliveFailure = 12,
        Pending = 13,
        Timeout = 14,
        ProxyNameResolutionFailure = 15,
        UnknownError = 16,
        MessageLengthLimitExceeded = 17,
        CacheEntryNotFound = 18,
        RequestProhibitedByCachePolicy = 19,
        RequestProhibitedByProxy = 20,
    }
    public abstract partial class WebRequest : System.MarshalByRefObject, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        protected WebRequest() { }
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected WebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public System.Net.Security.AuthenticationLevel AuthenticationLevel { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.Cache.RequestCachePolicy? CachePolicy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual string? ConnectionGroupName { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual long ContentLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual string? ContentType { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public virtual System.Net.ICredentials? Credentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.Cache.RequestCachePolicy? DefaultCachePolicy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.IWebProxy? DefaultWebProxy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual string Method { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual bool PreAuthenticate { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.IWebProxy? Proxy { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Uri RequestUri { get { throw new System.PlatformNotSupportedException(); } }
        public virtual int Timeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual bool UseDefaultCredentials { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual void Abort() { }
        public virtual System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        public virtual System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.WebRequest Create(string requestUriString) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.WebRequest Create(System.Uri requestUri) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.WebRequest CreateDefault(System.Uri requestUri) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.HttpWebRequest CreateHttp(string requestUriString) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.HttpWebRequest CreateHttp(System.Uri requestUri) { throw new System.PlatformNotSupportedException(); }
        public virtual System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        public virtual System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for WebRequest.")]
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public virtual System.IO.Stream GetRequestStream() { throw new System.PlatformNotSupportedException(); }
        public virtual System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { throw new System.PlatformNotSupportedException(); }
        public virtual System.Net.WebResponse GetResponse() { throw new System.PlatformNotSupportedException(); }
        public virtual System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { throw new System.PlatformNotSupportedException(); }
        public static System.Net.IWebProxy GetSystemWebProxy() { throw new System.PlatformNotSupportedException(); }
        public static bool RegisterPrefix(string prefix, System.Net.IWebRequestCreate creator) { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for WebRequest.")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public static partial class WebRequestMethods
    {
        public static partial class File
        {
            public const string DownloadFile = "GET";
            public const string UploadFile = "PUT";
        }
        public static partial class Ftp
        {
            public const string AppendFile = "APPE";
            public const string DeleteFile = "DELE";
            public const string DownloadFile = "RETR";
            public const string GetDateTimestamp = "MDTM";
            public const string GetFileSize = "SIZE";
            public const string ListDirectory = "NLST";
            public const string ListDirectoryDetails = "LIST";
            public const string MakeDirectory = "MKD";
            public const string PrintWorkingDirectory = "PWD";
            public const string RemoveDirectory = "RMD";
            public const string Rename = "RENAME";
            public const string UploadFile = "STOR";
            public const string UploadFileWithUniqueName = "STOU";
        }
        public static partial class Http
        {
            public const string Connect = "CONNECT";
            public const string Get = "GET";
            public const string Head = "HEAD";
            public const string MkCol = "MKCOL";
            public const string Post = "POST";
            public const string Put = "PUT";
        }
    }
    public abstract partial class WebResponse : System.MarshalByRefObject, System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        protected WebResponse() { }
        [System.ObsoleteAttribute("Serialization has been deprecated for WebResponse.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected WebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public virtual long ContentLength { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual string ContentType { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Net.WebHeaderCollection Headers { get { throw new System.PlatformNotSupportedException(); } }
        public virtual bool IsFromCache { get { throw new System.PlatformNotSupportedException(); } }
        public virtual bool IsMutuallyAuthenticated { get { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Uri ResponseUri { get { throw new System.PlatformNotSupportedException(); } }
        public virtual bool SupportsHeaders { get { throw new System.PlatformNotSupportedException(); } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        [System.ObsoleteAttribute("Serialization has been deprecated for WebResponse.")]
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public virtual System.IO.Stream GetResponseStream() { throw new System.PlatformNotSupportedException(); }
        [System.ObsoleteAttribute("Serialization has been deprecated for WebResponse.")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public delegate System.Net.IPEndPoint BindIPEndPoint(System.Net.ServicePoint servicePoint, System.Net.IPEndPoint remoteEndPoint, int retryCount);
    [System.FlagsAttribute]
    public enum SecurityProtocolType
    {
        SystemDefault = 0,
        [System.ObsoleteAttribute("SecurityProtocolType.Ssl3 has been deprecated and is not supported.")]
        Ssl3 = 48,
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072,
        Tls13 = 12288,
    }
    public partial class ServicePoint
    {
        internal ServicePoint() { }
        public System.Uri Address { get { throw new System.PlatformNotSupportedException(); } }
        public System.Net.BindIPEndPoint? BindIPEndPointDelegate { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate? Certificate { get { throw new System.PlatformNotSupportedException(); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate? ClientCertificate { get { throw new System.PlatformNotSupportedException(); } }
        public int ConnectionLeaseTimeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public int ConnectionLimit { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public string ConnectionName { get { throw new System.PlatformNotSupportedException(); } }
        public int CurrentConnections { get { throw new System.PlatformNotSupportedException(); } }
        public bool Expect100Continue { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public System.DateTime IdleSince { get { throw new System.PlatformNotSupportedException(); } }
        public int MaxIdleTime { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public virtual System.Version ProtocolVersion { get { throw new System.PlatformNotSupportedException(); } }
        public int ReceiveBufferSize { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool SupportsPipelining { get { throw new System.PlatformNotSupportedException(); } }
        public bool UseNagleAlgorithm { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public bool CloseConnectionGroup(string connectionGroupName) { throw new System.PlatformNotSupportedException(); }
        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { }
    }
    [System.ObsoleteAttribute("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead. Settings on ServicePointManager no longer affect SslStream or HttpClient.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public partial class ServicePointManager
    {
        internal ServicePointManager() { }
        public const int DefaultNonPersistentConnectionLimit = 4;
        public const int DefaultPersistentConnectionLimit = 2;
        public static bool CheckCertificateRevocationList { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static int DefaultConnectionLimit { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static int DnsRefreshTimeout { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static bool EnableDnsRoundRobin { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
        public static System.Net.Security.EncryptionPolicy EncryptionPolicy { get { throw new System.PlatformNotSupportedException(); } }
        public static bool Expect100Continue { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static int MaxServicePointIdleTime { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static int MaxServicePoints { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static bool ReusePort { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.SecurityProtocolType SecurityProtocol { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.Security.RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static bool UseNagleAlgorithm { get { throw new System.PlatformNotSupportedException(); } set { throw new System.PlatformNotSupportedException(); } }
        public static System.Net.ServicePoint FindServicePoint(string uriString, System.Net.IWebProxy? proxy) { throw new System.PlatformNotSupportedException(); }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address) { throw new System.PlatformNotSupportedException(); }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address, System.Net.IWebProxy? proxy) { throw new System.PlatformNotSupportedException(); }
        public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { }
    }
}
namespace System.Net.Cache
{
    public enum HttpCacheAgeControl
    {
        None = 0,
        MinFresh = 1,
        MaxAge = 2,
        MaxAgeAndMinFresh = 3,
        MaxStale = 4,
        MaxAgeAndMaxStale = 6,
    }
    public enum HttpRequestCacheLevel
    {
        Default = 0,
        BypassCache = 1,
        CacheOnly = 2,
        CacheIfAvailable = 3,
        Revalidate = 4,
        Reload = 5,
        NoCacheNoStore = 6,
        CacheOrNextCacheOnly = 7,
        Refresh = 8,
    }
    public partial class HttpRequestCachePolicy : System.Net.Cache.RequestCachePolicy
    {
        public HttpRequestCachePolicy() { }
        public HttpRequestCachePolicy(System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan ageOrFreshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale, System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel level) { }
        public System.DateTime CacheSyncDate { get { throw new System.PlatformNotSupportedException(); } }
        public new System.Net.Cache.HttpRequestCacheLevel Level { get { throw new System.PlatformNotSupportedException(); } }
        public System.TimeSpan MaxAge { get { throw new System.PlatformNotSupportedException(); } }
        public System.TimeSpan MaxStale { get { throw new System.PlatformNotSupportedException(); } }
        public System.TimeSpan MinFresh { get { throw new System.PlatformNotSupportedException(); } }
        public override string ToString() { throw new System.PlatformNotSupportedException(); }
    }
}
