using System;

namespace Window
{
    /// <summary>
    /// WebRTC peer connection (minimal surface).
    /// </summary>
    [NetJs.External]
    public class RTCPeerConnection : EventTarget
    {
        public extern RTCPeerConnection(object? config = null);
        public extern Promise<object> createOffer();
        public extern Promise<object> createAnswer();
        public extern Promise<object> setLocalDescription(object desc);
        public extern Promise<object> setRemoteDescription(object desc);
        public extern Promise<object> addIceCandidate(object candidate);
        public extern MediaStream[] getLocalStreams();
        public extern MediaStream[] getRemoteStreams();
        public extern void addTrack(MediaStreamTrack track, MediaStream stream);
        public extern void removeTrack(object sender);
        public extern void close();
    }
}