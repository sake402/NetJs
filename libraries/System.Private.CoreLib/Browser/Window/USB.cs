using System;

namespace Window
{
    /// <summary>
    /// WebUSB minimal wrappers.
    /// </summary>
    [NetJs.External]
    public class USB
    {
        public extern Promise<USBDevice[]> getDevices();
        public extern Promise<USBDevice> requestDevice(object options);
    }

    [NetJs.External]
    public class USBConfiguration
    {
        public extern int configurationValue { get; }
        public extern USBInterface[]? interfaces { get; }
    }

    [NetJs.External]
    public class USBInterface
    {
        public extern int interfaceNumber { get; }
        public extern USBAlternateInterface[]? alternates { get; }
    }

    [NetJs.External]
    public class USBAlternateInterface
    {
        public extern int alternateSetting { get; }
        public extern int interfaceClass { get; }
        public extern int interfaceSubclass { get; }
        public extern int interfaceProtocol { get; }
        public extern USBEndpoint[]? endpoints { get; }
    }

    [NetJs.External]
    public class USBEndpoint
    {
        public extern int endpointNumber { get; }
        public extern string? direction { get; }
        public extern string? type { get; }
    }

    [NetJs.External]
    public class USBInTransferResult
    {
        public extern DataView? data { get; }
        public extern string? status { get; }
    }

    [NetJs.External]
    public class USBOutTransferResult
    {
        public extern int bytesWritten { get; }
        public extern string? status { get; }
    }

    [NetJs.External]
    public class USBDevice : EventTarget
    {
        public extern string? productName { get; }
        public extern string? manufacturerName { get; }
        public extern int? vendorId { get; }
        public extern int? productId { get; }
        public extern string? serialNumber { get; }
        public extern int? deviceClass { get; }
        public extern int? deviceSubclass { get; }
        public extern int? deviceProtocol { get; }
        public extern USBConfiguration? configuration { get; }
        public extern USBConfiguration[]? configurations { get; }
        public extern Promise<object> open();
        public extern Promise<object> close();
        public extern Promise<object> transferOut(int endpointNumber, object data);
        public extern Promise<USBInTransferResult> transferIn(int endpointNumber, int length);
        public extern Promise<USBInTransferResult> controlTransferIn(object setup, int length);
        public extern Promise<USBOutTransferResult> controlTransferOut(object setup, object? data = null);
        public extern Promise<object> selectConfiguration(int configurationValue);
        public extern Promise<object> claimInterface(int interfaceNumber);
        public extern Promise<object> releaseInterface(int interfaceNumber);
        public extern Promise<object> selectAlternateInterface(int interfaceNumber, int alternateSetting);
        public extern Promise<object> reset();
        public extern Promise<object> clearHalt(int endpointNumber);
        public extern bool opened { get; }
    }
}