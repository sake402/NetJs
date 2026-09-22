using System;

namespace NetJs
{
    [Flags]
    [NonScriptable]
    public enum KeepDependencyFlags
    {
        None,
        TargetConstructorCalled = 1 << 0,
        TargetTypeUsed = 1 << 1,
        KeepConstructorIfTypeKept = 1 << 2
    }
}
