namespace Analyze.Model
{
    public sealed class NativeThread
    {
        public uint ThreadId { get; }
        public string Name { get; }
        public uint SuspendCount { get; }
        public uint PriorityClass { get; }
        public uint Priority { get; }
        public byte[] ContextData { get; }
        public ulong Teb { get; }

        public NativeThread(uint threadId, string name, uint suspendCount, uint priorityClass, uint priority, byte[] contextData, ulong teb)
        {
            ThreadId = threadId;
            Name = name;
            SuspendCount = suspendCount;
            PriorityClass = priorityClass;
            Priority = priority;
            ContextData = contextData;
            Teb = teb;
        }
    }
}
