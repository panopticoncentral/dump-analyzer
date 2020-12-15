using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

using Analyze.Model;

namespace Analyze
{
    internal static unsafe class Analyzer
    {
        private struct View : IDisposable
        {
            private readonly MemoryMappedFile _file;

            private ulong _currentViewRva;
            private MemoryMappedViewAccessor _currentView;

            public View(string path)
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                _file = MemoryMappedFile.CreateFromFile(stream, mapName: null, capacity: 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);

                // Start by mapping in first 64k, as these can be very big.
                _currentViewRva = 0;
                _currentView = _file.CreateViewAccessor(offset: 0, size: 64 * 1024, MemoryMappedFileAccess.Read);
            }

            public nint GetPointer(ulong rva, ulong size)
            {
                if ((rva < _currentViewRva) || ((rva + size) > (_currentViewRva + (ulong)_currentView.Capacity)))
                {
                    _currentView.Dispose();
                    _currentViewRva = rva;
                    _currentView = _file.CreateViewAccessor(offset: (long)rva, size: (long)Math.Max(64ul * 1024, size), MemoryMappedFileAccess.Read);
                }

                return _currentView.SafeMemoryMappedViewHandle.DangerousGetHandle() + (nint)_currentView.PointerOffset + (nint)(rva - _currentViewRva);
            }

            public nint GetPointer(Native.MinidumpLocationDescriptor location) => GetPointer(location.Rva, location.DataSize);

            public void Dispose()
            {
                _currentView.Dispose();
                _file.Dispose();
            }
        }

        public static Dump Analyze(string path)
        {
            using var view = new View(path);

            var header = (Native.MinidumpHeader*)view.GetPointer(0, (uint)sizeof(Native.MinidumpHeader));
            var timestamp = Utilities.FromTimeT(header->TimeDateStamp);
            var type = header->Flags;
            var directoryRva = header->StreamDirectoryRva;
            var directoryCount = header->NumberOfStreams;

            var directory = ReadList<Native.MinidumpDirectory>(view, directoryRva, directoryCount);

            var architecture = ReadStreamStruct<Native.MinidumpSystemInfo>(view, directory, Native.MinidumpStreamType.SystemInfo).ProcessorArchitecture;

            var modules = GetModules(view, directory);
            var threads = GetThreads(view, directory);
            var memoryInfos = GetMemoryInfos(view, directory, architecture);

            return new Dump(path, timestamp, type, architecture, modules, threads, memoryInfos);
        }

        private static IReadOnlyList<NativeModule> GetModules(View view, IReadOnlyList<Native.MinidumpDirectory> directory)
        {
            var miniDumpModules = ReadStreamList<Native.MinidumpModule>(view, directory, Native.MinidumpStreamType.ModuleList);

            NativeModule ToNativeModule(Native.MinidumpModule miniModule)
            {
                var name = ReadString(view, miniModule.ModuleNameRva);
                var cvData = ReadBuffer(view, miniModule.CvRecord);
                var version = new Version(
                    (int)((miniModule.VersionInfo.FileVersionMs >> 16) & 0xffff),
                    (int)(miniModule.VersionInfo.FileVersionMs & 0xffff),
                    (int)((miniModule.VersionInfo.FileVersionLs >> 16) & 0xffff),
                    (int)(miniModule.VersionInfo.FileVersionLs & 0xffff));
                return new NativeModule(name, miniModule.BaseOfImage, miniModule.SizeOfImage, Utilities.FromTimeT(miniModule.TimeDateStamp), version, cvData);
            }

            return ImmutableList<NativeModule>.Empty.AddRange(miniDumpModules.Select(ToNativeModule));
        }

        private static IReadOnlyList<NativeThread> GetThreads(View view, IReadOnlyList<Native.MinidumpDirectory> directory)
        {
            var threadNames = ReadStreamList<Native.MiniDumpThreadName>(view, directory, Native.MinidumpStreamType.ThreadNames);
            var threadNameMap = threadNames.ToDictionary(tn => tn.ThreadId, tn => ReadString(view, tn.RvaOfThreadName));

            var threads = ReadStreamList<Native.MinidumpThread>(view, directory, Native.MinidumpStreamType.ThreadList);

            NativeThread ToNativeThread(Native.MinidumpThread miniThread)
            {
                if (!threadNameMap.TryGetValue(miniThread.ThreadId, out var name))
                {
                    name = string.Empty;
                }

                var contextData = ReadBuffer(view, miniThread.ThreadContext);

                return new NativeThread(miniThread.ThreadId, name, miniThread.SuspendCount, miniThread.PriorityClass, miniThread.Priority, contextData, miniThread.Teb);
            }

            return ImmutableList<NativeThread>.Empty.AddRange(threads.Select(ToNativeThread));
        }

        private static IReadOnlyList<NativeMemoryInfo> GetMemoryInfos(View view, IReadOnlyList<Native.MinidumpDirectory> directory, ProcessorArchitecture architecture)
        {
            var infos = ReadStreamList<Native.MiniDumpMemoryInfoList, Native.MiniDumpMemoryInfo>(view, directory, Native.MinidumpStreamType.MemoryInfoList, h => (uint)h.NumberOfEntries);

            NativeMemoryInfo ToNativeMemoryInfo(Native.MiniDumpMemoryInfo miniMemoryInfo)
            {
                return new NativeMemoryInfo(Utilities.PointerSize(architecture) == 4 ? miniMemoryInfo.BaseAddress & 0x00000000FFFFFFFFUL : miniMemoryInfo.BaseAddress,
                                            Utilities.PointerSize(architecture) == 4 ? miniMemoryInfo.AllocationBase & 0x00000000FFFFFFFFUL : miniMemoryInfo.AllocationBase,
                                            (MemoryProtection)miniMemoryInfo.AllocationProtect,
                                            miniMemoryInfo.RegionSize,
                                            (MemoryState)miniMemoryInfo.State,
                                            (MemoryProtection)miniMemoryInfo.Protect,
                                            (MemoryType)miniMemoryInfo.Type);
            }

            return ImmutableList<NativeMemoryInfo>.Empty.AddRange(infos.Select(ToNativeMemoryInfo));
        }

        private static string ReadString(View view, ulong rva)
        {
            // We're assuming most strings will be <4k, revisit if necessary.
            const int DefaultSize = 4096;

            var start = view.GetPointer(rva, DefaultSize);
            var length = *(uint*)start;
            if (length > DefaultSize)
            {
                start = view.GetPointer(rva, sizeof(uint) + length);
            }

            var s = Marshal.PtrToStringUni(new IntPtr((void*)(start + sizeof(uint))), (int)(length / 2));
            return s;
        }

        private static byte[] ReadBuffer(View view, Native.MinidumpLocationDescriptor location)
        {
            if (location.DataSize == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[location.DataSize];
            Marshal.Copy(view.GetPointer(location), buffer, startIndex: 0, buffer.Length);
            return buffer;
        }

        private static bool FindStream(IReadOnlyList<Native.MinidumpDirectory> directory, Native.MinidumpStreamType streamType, out Native.MinidumpLocationDescriptor location)
        {
            var directoryEntry = directory.Where(d => d.StreamType == streamType).SingleOrDefault();
            location = directoryEntry.Location;
            return directoryEntry.StreamType == streamType;
        }

        private static T ReadStreamStruct<T>(View view, IReadOnlyList<Native.MinidumpDirectory> directory, Native.MinidumpStreamType streamType) where T : unmanaged =>
            !FindStream(directory, streamType, out var location) || location.DataSize != sizeof(T) ? default : *(T*)view.GetPointer(location);

        private static IReadOnlyList<T> ReadStreamList<T>(View view, IReadOnlyList<Native.MinidumpDirectory> directory, Native.MinidumpStreamType streamType) where T : unmanaged
        {
            if (!FindStream(directory, streamType, out var location) || location.DataSize < sizeof(uint))
            {
                return ImmutableList<T>.Empty;
            }

            var start = view.GetPointer(location);
            var count = *(uint*)start;

            if (location.DataSize != (sizeof(uint) + (count * sizeof(T))))
            {
                return ImmutableList<T>.Empty;
            }

            var list = ImmutableList<T>.Empty;
            for (var i = 0; i < count; i++)
            {
                list = list.Add(*(T*)(start + sizeof(uint) + (i * sizeof(T))));
            }

            return list;
        }

        private static IReadOnlyList<TValue> ReadStreamList<THeader, TValue>(View view, IReadOnlyList<Native.MinidumpDirectory> directory, Native.MinidumpStreamType streamType, Func<THeader, uint> countAccessor)
            where THeader : unmanaged
            where TValue : unmanaged
        {
            if (!FindStream(directory, streamType, out var location) || location.DataSize < sizeof(THeader))
            {
                return ImmutableList<TValue>.Empty;
            }

            var start = view.GetPointer(location);
            var count = countAccessor(*(THeader*)start);

            if (location.DataSize != (sizeof(THeader) + (count * sizeof(TValue))))
            {
                return ImmutableList<TValue>.Empty;
            }

            var list = ImmutableList<TValue>.Empty;
            for (var i = 0; i < count; i++)
            {
                list = list.Add(*(TValue*)(start + sizeof(THeader) + (i * sizeof(TValue))));
            }

            return list;
        }

        private static IReadOnlyList<T> ReadList<T>(View view, uint rva, uint count) where T : unmanaged
        {
            var start = view.GetPointer(rva, count * (uint)sizeof(T));

            var list = ImmutableList<T>.Empty;
            for (var i = 0; i < count; i++)
            {
                list = list.Add(*(T*)(start + (i * sizeof(T))));
            }

            return list;
        }

    }
}
