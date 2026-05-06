using System;

namespace Window
{
    [NetJs.External]
    public class MediaDevices
    {
        public extern Promise<MediaStream> getUserMedia(object? constraints = null);
        public extern Promise<MediaDeviceInfo[]> enumerateDevices();
    }

    [NetJs.External]
    public class MediaDeviceInfo
    {
        public extern string deviceId { get; }
        public extern string kind { get; }
        public extern string label { get; }
        public extern string groupId { get; }
    }
}