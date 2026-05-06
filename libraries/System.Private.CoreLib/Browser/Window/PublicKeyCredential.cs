using System;

namespace Window
{
    /// <summary>
    /// WebAuthn PublicKeyCredential wrapper.
    /// </summary>
    [NetJs.External]
    public class PublicKeyCredential : Credential
    {
        public extern byte[]? rawId { get; }
        public extern object? response { get; }
        public extern object? getClientExtensionResults();
        public static extern Promise<PublicKeyCredential> get(object options);
        public static extern Promise<CredentialCreationResult> create(object options);
    }

    [NetJs.External]
    public class Credential
    {
        public extern string? id { get; }
        public extern string? type { get; }
        public extern object? rawId { get; }
    }

    [NetJs.External]
    public class CredentialCreationResult
    {
        public extern PublicKeyCredential credential { get; }
    }
}