using System;

namespace Window
{
    /// <summary>
    /// Web Bluetooth minimal wrappers.
    /// </summary>
    [NetJs.External]
    public class Bluetooth
    {
        public extern Promise<BluetoothDevice> requestDevice(object options);
    }

    [NetJs.External]
    public class BluetoothDevice : EventTarget
    {
        public extern string? id { get; }
        public extern string? name { get; }
        public extern Promise<BluetoothRemoteGATTServer> gatt { get; }
        public extern Promise<object> watchAdvertisements();
        public extern Promise<object> forget();
    }

    [NetJs.External]
    public class BluetoothRemoteGATTServer
    {
        public extern Promise<object> connect();
        public extern Promise<object> disconnect();
        public extern Promise<BluetoothRemoteGATTService> getPrimaryService(string service);
        public extern bool connected { get; }
    }

    [NetJs.External]
    public class BluetoothRemoteGATTService
    {
        public extern Promise<BluetoothRemoteGATTCharacteristic> getCharacteristic(string uuid);
    }

    [NetJs.External]
    public class BluetoothRemoteGATTCharacteristic
    {
        public extern Promise<object> readValue();
        public extern Promise<object> writeValue(object value);
        public extern Promise<object> startNotifications();
        public extern Promise<object> stopNotifications();
    }
}