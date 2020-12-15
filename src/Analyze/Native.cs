using System.Diagnostics;
using System.Runtime.InteropServices;

using Analyze.Model;

namespace Analyze
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MinidumpHeader
        {
            public uint Signature { get; }
            public uint Version { get; }
            public uint NumberOfStreams { get; }
            public uint StreamDirectoryRva { get; }
            public uint CheckSum { get; }
            public uint TimeDateStamp { get; }
            public DumpType Flags { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MinidumpLocationDescriptor
        {
            public uint DataSize { get; }
            public uint Rva { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [DebuggerDisplay("{StreamType}")]
        public readonly struct MinidumpDirectory
        {
            public MinidumpStreamType StreamType { get; }
            public MinidumpLocationDescriptor Location { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct CpuInformation
        {
            public uint VendorId1 { get; }
            public uint VendorId2 { get; }
            public uint VendorId3 { get; }
            public uint VersionInformation { get; }
            public uint FeatureInformation { get; }
            public uint AmdExtendedCpuFeatures { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MinidumpSystemInfo
        {
            public ProcessorArchitecture ProcessorArchitecture { get; }
            public ushort ProcessorLevel { get; }
            public ushort ProcessorRevision { get; }
            public byte NumberOfProcessors { get; }
            public byte ProductType { get; }
            public uint MajorVersion { get; }
            public uint MinorVersion { get; }
            public uint BuildNumber { get; }
            public uint PlatformId { get; }
            public uint CsdVersionRva { get; }
            public ushort SuiteMask { get; }
            private readonly ushort _reserved;
            public CpuInformation CpuInformation { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VsFixedFileInfo
        {
            public uint Signature { get; }
            public uint StrucVersion { get; }
            public uint FileVersionMs { get; }
            public uint FileVersionLs { get; }
            public uint ProductVersionMs { get; }
            public uint ProductVersionLs { get; }
            public uint FileFlagsMask { get; }
            public uint FileFlags { get; }
            public uint FileOs { get; }
            public uint FileType { get; }
            public uint FileSubtype { get; }
            public uint FileDateMs { get; }
            public uint FileDateLs { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MinidumpModule
        {
            public ulong BaseOfImage { get; }
            public uint SizeOfImage { get; }
            public uint CheckSum { get; }
            public uint TimeDateStamp { get; }
            public uint ModuleNameRva { get; }
            public VsFixedFileInfo VersionInfo { get; }
            public MinidumpLocationDescriptor CvRecord { get; }
            public MinidumpLocationDescriptor MiscRecord { get; }
            private readonly ulong _reserved0;
            private readonly ulong _reserved1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MiniDumpThreadName
        {
            public uint ThreadId { get; }
            public ulong RvaOfThreadName { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MinidumpMemoryDescriptor
        {
            public ulong StartOfMemoryRange { get; }
            public MinidumpLocationDescriptor Memory { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MinidumpThread
        {
            public uint ThreadId { get; }
            public uint SuspendCount { get; }
            public uint PriorityClass { get; }
            public uint Priority { get; }
            public ulong Teb { get; }
            public MinidumpMemoryDescriptor Stack { get; }
            public MinidumpLocationDescriptor ThreadContext { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MiniDumpMemoryInfoList
        {
            public uint SizeOfHeader { get; }
            public uint SizeOfEntry { get; }
            public ulong NumberOfEntries { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MiniDumpMemoryInfo
        {
            public ulong BaseAddress { get; }
            public ulong AllocationBase { get; }
            public uint AllocationProtect { get; }
            private readonly uint _alignment1;
            public ulong RegionSize { get; }
            public uint State { get; }
            public uint Protect { get; }
            public uint Type { get; }
            private readonly uint _alignment2;
        }

        public enum MinidumpStreamType : uint
        {
            Unused,
            Reserved0,
            Reserved1,
            ThreadList,
            ModuleList,
            MemoryList,
            Exception,
            SystemInfo,
            ThreadExList,
            Memory64List,
            CommentAnsi,
            CommentUnicode,
            HandleData,
            FunctionTable,
            UnloadedModuleList,
            MiscInfo,
            MemoryInfoList,
            ThreadInfoList,
            HandleOperationList,
            Token,
            JavaScriptData,
            SystemMemoryInfo,
            ProcessVmCounters,
            IptTrace,
            ThreadNames,
            ceNull,
            ceSystemInfo,
            ceException,
            ceModuleList,
            ceProcessList,
            ceThreadList,
            ceThreadContextList,
            ceThreadCallStackList,
            ceMemoryVirtualList,
            ceMemoryPhysicalList,
            ceBucketParameters,
            ceProcessModuleMap,
            ceDiagnosisList,
            LastReserved
        }

        [DllImport("Dbghelp.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MiniDumpReadDumpStream(nint baseOfDump, MinidumpStreamType streamNumber, out MinidumpDirectory dir, out nint streamPointer, out uint streamSize);
    }
}
