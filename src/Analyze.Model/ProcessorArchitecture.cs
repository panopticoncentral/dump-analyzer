namespace Analyze.Model
{
    public enum ProcessorArchitecture : ushort
    {
        X86 = 0,
        Mips = 1,
        Alpha = 2,
        Ppc = 3,
        Shx = 4,
        Arm = 5,
        Ia64 = 6,
        Alpha64 = 7,
        Msil = 8,
        X64 = 9,
        Ia32OnWin64 = 10,
        Neutral = 11,
        Arm64 = 12,
        Arm32OnWin64 = 13,
        Ia32OnArm64 = 14,
        Unknown = 0xFFFF
    }
}
