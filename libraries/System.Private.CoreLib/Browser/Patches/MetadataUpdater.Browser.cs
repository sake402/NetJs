using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection.Metadata
{
    [NetJs.ForcePartial(typeof(MetadataUpdater))]
    public static class MetadataUpdater_Partial
    {
        [NetJs.MemberReplace]
        private static int ApplyUpdateEnabled(int justComponentCheck) => 0;

        [NetJs.MemberReplace]
        private static string GetApplyUpdateCapabilities() => null!;

    }
}
