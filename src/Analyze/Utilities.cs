using System;

using Analyze.Model;

namespace Analyze
{
    internal static class Utilities
    {
        public static uint PointerSize(this ProcessorArchitecture architecture) =>
            architecture switch
            {
                ProcessorArchitecture.Ia32OnWin64 or ProcessorArchitecture.Ia32OnArm64 or ProcessorArchitecture.X86 => 4,
                ProcessorArchitecture.Arm64 or ProcessorArchitecture.Ia64 or ProcessorArchitecture.X64 => 8,
                _ => throw new ArgumentException($"Unexpected architecture type: {architecture}"),
            };

        public static DateTime FromTimeT(uint timet) => new DateTime(1970, 1, 1).AddSeconds(timet);
    }
}
