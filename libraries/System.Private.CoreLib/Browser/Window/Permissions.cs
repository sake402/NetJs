using System;

namespace Window
{
    [NetJs.External]
    public class Permissions
    {
        public extern Promise<PermissionStatus> query(object desc);
        public extern Promise<object> request(object desc);
    }

    [NetJs.External]
    public class PermissionStatus : EventTarget
    {
        public extern string state { get; }
        public extern void onchange(object? handler);
    }
}