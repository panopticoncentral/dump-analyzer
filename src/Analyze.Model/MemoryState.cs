namespace Analyze.Model
{
    public enum MemoryState
    {
        None,
        Committed = 0x1000,
        Free = 0x10000,
        Reserved = 0x2000
    }
}
