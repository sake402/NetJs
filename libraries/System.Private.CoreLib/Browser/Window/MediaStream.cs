using System;

namespace Window
{
    /// <summary>
    /// Media stream (getUserMedia).
    /// </summary>
    [NetJs.External]
    public class MediaStream : EventTarget
    {
        public extern MediaStreamTrack[] getTracks();
        public extern MediaStreamTrack[] getAudioTracks();
        public extern MediaStreamTrack[] getVideoTracks();
        public extern void addTrack(MediaStreamTrack track);
        public extern void removeTrack(MediaStreamTrack track);
    }

    [NetJs.External]
    public class MediaStreamTrack : EventTarget
    {
        public extern string id { get; }
        public extern string kind { get; }
        public extern bool enabled { get; set; }
        public extern string? label { get; }
        public extern void stop();
    }
}