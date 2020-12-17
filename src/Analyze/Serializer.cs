using System;
using System.Collections.Generic;
using System.IO;

using Analyze.Model;

namespace Analyze
{
    internal sealed class Serializer : IDisposable
    {
        public const uint Version = 0x1;

        private readonly FileStream _stream;
        private readonly BinaryWriter _writer;

        private long _end;
        private SerializationStream? _currentStream;

        public string Filename { get; }

        public Serializer(string filename)
        {
            Filename = filename;
            _stream = new FileStream(filename, FileMode.Create);
            _writer = new BinaryWriter(_stream);
            _writer.Write(Version);
            _end = sizeof(uint) + (sizeof(ulong) * (int)StreamType.Max);
        }

        private void Serialize(ushort u) => _writer.Write(u);
        private void Serialize(int i) => _writer.Write(i);
        private void Serialize(ulong u) => _writer.Write(u);
        private void Serialize(string s) => _writer.Write(s);
        private void Serialize(DateTime d) => _writer.Write(d.Ticks);
        private void Serialize(byte[] buffer) => _writer.Write(buffer);
        private void Serialize<T>(IReadOnlyCollection<T> collection, Action<T> serialize)
        {
            Serialize(collection.Count);
            foreach (var element in collection)
            {
                serialize(element);
            }
        }

        private void Serialize(NativeModule m)
        {
            Serialize(m.Name);
            Serialize(m.BaseOfImage);
            Serialize(m.SizeOfImage);
            Serialize(m.Timestamp);
            Serialize(m.Version.Major);
            Serialize(m.Version.Minor);
            Serialize(m.Version.Build);
            Serialize(m.Version.Revision);
            Serialize(m.CvData);
        }

        private void Serialize(NativeThread t)
        {
            Serialize(t.ThreadId);
            Serialize(t.Name);
            Serialize(t.SuspendCount);
            Serialize(t.PriorityClass);
            Serialize(t.Priority);
            Serialize(t.ContextData);
            Serialize(t.Teb);
        }

        private void Serialize(NativeMemoryInfo m)
        {
            Serialize(m.BaseAddress);
            Serialize(m.AllocationBase);
            Serialize((int)m.AllocationProtect);
            Serialize(m.RegionSize);
            Serialize((int)m.State);
            Serialize((int)m.Protect);
            Serialize((int)m.Type);
        }

        public void Serialize(Dump dump)
        {
            {
                using SerializationStream infoStream = new(this, StreamType.Info);
                Serialize(dump.Path);
                Serialize(dump.Timestamp);
                Serialize((ulong)dump.Type);
                Serialize((ushort)dump.Architecture);
            }

            {
                using SerializationStream moduleStream = new(this, StreamType.NativeModules);
                Serialize(dump.Modules, Serialize);
            }

            {
                using SerializationStream threadStream = new(this, StreamType.NativeThreads);
                Serialize(dump.Threads, Serialize);
            }

            {
                using SerializationStream memoryInfoStream = new(this, StreamType.NativeMemoryInfos);
                Serialize(dump.MemoryInfos, Serialize);
            }
        }

        public void Dispose()
        {
            _writer.Close();
            _stream.Close();
        }

        private enum StreamType
        {
            Invalid,
            Info,
            NativeModules,
            NativeThreads,
            NativeMemoryInfos,
            Max
        }

        private sealed class SerializationStream : IDisposable
        {
            private readonly Serializer _serializer;

            public SerializationStream(Serializer serializer, StreamType type)
            {
                if (serializer._currentStream != null)
                {
                    throw new InvalidOperationException();
                }

                serializer._currentStream = this;

                _ = serializer._stream.Seek(sizeof(uint) + (sizeof(ulong) * ((int)type - 1)), SeekOrigin.Begin);
                serializer._writer.Write(serializer._end);
                _ = serializer._stream.Seek(serializer._end, SeekOrigin.Begin);
                _serializer = serializer;
            }

            public void Dispose()
            {
                _serializer._currentStream = null;
                _serializer._end = _serializer._stream.Position;
            }
        }

        private sealed class Bookmark : IDisposable
        {
            private readonly Stream _stream;
            private readonly long _location;

            public Bookmark(Stream stream)
            {
                _stream = stream;
                _location = stream.Position;
            }

            public void Dispose() => _ = _stream.Seek(_location, SeekOrigin.Begin);
        }
    }
}
