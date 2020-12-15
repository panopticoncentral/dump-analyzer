using System;
using System.Collections.Generic;

namespace Analyze.Model
{
    public sealed class Dump
    {
        public string Path { get; }

        public DateTime Timestamp { get; }

        public DumpType Type { get; }

        public ProcessorArchitecture Architecture { get; }

        public IReadOnlyList<NativeModule> Modules { get; }

        public IReadOnlyList<NativeThread> Threads { get; }

        public IReadOnlyList<NativeMemoryInfo> MemoryInfos { get; }

        public Dump(string path, DateTime timestamp, DumpType type, ProcessorArchitecture architecture, IReadOnlyList<NativeModule> modules, IReadOnlyList<NativeThread> threads, IReadOnlyList<NativeMemoryInfo> memoryInfos)
        {
            Path = path;
            Timestamp = timestamp;
            Type = type;
            Architecture = architecture;
            Modules = modules;
            Threads = threads;
            MemoryInfos = memoryInfos;
        }
    }
}
