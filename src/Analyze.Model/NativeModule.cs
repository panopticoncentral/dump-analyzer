using System;

namespace Analyze.Model
{
    public sealed class NativeModule
    {
        public string Name { get; }
        public ulong BaseOfImage { get; }
        public uint SizeOfImage { get; }
        public DateTime Timestamp { get; }
        public Version Version { get; }
        public byte[] CvData { get; }

        public NativeModule(string name, ulong baseOfImage, uint sizeOfImage, DateTime timestamp, Version version, byte[] cvData)
        {
            Name = name;
            BaseOfImage = baseOfImage;
            SizeOfImage = sizeOfImage;
            Timestamp = timestamp;
            Version = version;
            CvData = cvData;
        }
    }
}
