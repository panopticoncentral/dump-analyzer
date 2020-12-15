namespace Analyze.Model
{
    public sealed class NativeMemoryInfo
    {
        public ulong BaseAddress { get; }
        public ulong AllocationBase { get; }
        public MemoryProtection AllocationProtect { get; }
        public ulong RegionSize { get; }
        public MemoryState State { get; }
        public MemoryProtection Protect { get; }
        public MemoryType Type { get; }

        public NativeMemoryInfo(ulong baseAddress, ulong allocationBase, MemoryProtection allocationProtect, ulong regionSize, MemoryState state, MemoryProtection protect, MemoryType type)
        {
            BaseAddress = baseAddress;
            AllocationBase = allocationBase;
            AllocationProtect = allocationProtect;
            RegionSize = regionSize;
            State = state;
            Protect = protect;
            Type = type;
        }
    }
}
