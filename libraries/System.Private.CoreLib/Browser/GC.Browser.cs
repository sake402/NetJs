using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace System
{
    public static partial class GC
    {
        [NetJs.MemberReplace(nameof(GetCollectionCount))]
        private static int GetCollectionCountImpl(int generation)
        {
            return 1;
        }

        [NetJs.MemberReplace(nameof(GetMaxGeneration))]
        private static int GetMaxGenerationImpl()
        {
            return 1;
        }

        [NetJs.MemberReplace(nameof(InternalCollect))]
        private static void InternalCollectImpl(int generation)
        {

        }

        [NetJs.MemberReplace(nameof(AddPressure))]
        private static void AddPressureImpl(ulong bytesAllocated)
        {

        }

        [NetJs.MemberReplace(nameof(RemovePressure))]
        private static void RemovePressureImpl(ulong bytesRemoved)
        {

        }

        // TODO: Move following to ConditionalWeakTable
        [NetJs.MemberReplace(nameof(register_ephemeron_array))]
        internal static void register_ephemeron_arrayImpl(Ephemeron[] array)
        {

        }

        [NetJs.MemberReplace(nameof(get_ephemeron_tombstone))]
        private static object get_ephemeron_tombstoneImpl()
        {
            return new object();
        }

        [NetJs.MemberReplace(nameof(RefreshMemoryLimit))]
        public static void RefreshMemoryLimitImpl()
        {
        }

        [NetJs.MemberReplace(nameof(RegisterNoGCRegionCallback))]
        public static void RegisterNoGCRegionCallbackImpl(long totalSize, Action callback)
        {
        }

        [NetJs.MemberReplace(nameof(GetAllocatedBytesForCurrentThread))]
        public static long GetAllocatedBytesForCurrentThreadImpl()
        {
            return 0;
        }

        [NetJs.MemberReplace(nameof(GetGeneration))]
        public static int GetGenerationImpl(object obj)
        {
            return 1;
        }

        [NetJs.MemberReplace(nameof(WaitForPendingFinalizers))]
        public static void WaitForPendingFinalizersImpl()
        {

        }

        [NetJs.MemberReplace(nameof(_SuppressFinalize))]
        private static void _SuppressFinalizeImpl(object o)
        {

        }

        [NetJs.MemberReplace(nameof(_ReRegisterForFinalize))]
        private static void _ReRegisterForFinalizeImpl(object o)
        {

        }

        [NetJs.MemberReplace(nameof(GetTotalMemory))]
        public static long GetTotalMemoryImpl(bool forceFullCollection)
        {
            return 0;
        }

        [NetJs.MemberReplace(nameof(_GetGCMemoryInfo))]
        private static void _GetGCMemoryInfoImpl(out long highMemoryLoadThresholdBytes,
                                        out long memoryLoadBytes,
                                        out long totalAvailableMemoryBytes,
                                        out long totalCommittedBytes,
                                        out long heapSizeBytes,
                                        out long fragmentedBytes)
        {
            highMemoryLoadThresholdBytes = 0;
            memoryLoadBytes = 0;
            totalAvailableMemoryBytes = 0;
            totalCommittedBytes = 0;
            heapSizeBytes = 0;
            fragmentedBytes = 0;
        }

        [NetJs.MemberReplace(nameof(AllocPinnedArray))]
        private static Array AllocPinnedArrayImpl(Type t, int length)
        {
            return Array.CreateFinal(t.As<RuntimeType>(), [length], null, null, 0);
        }


    }
}
