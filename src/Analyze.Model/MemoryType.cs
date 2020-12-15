namespace Analyze.Model
{
    public enum MemoryType
    {
        None,
        Image = 0x1000000,
        Mapped = 0x40000,
        Private = 0x20000
    }
}
