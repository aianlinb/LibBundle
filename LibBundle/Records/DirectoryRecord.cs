using System.Collections.Generic;

namespace LibBundle.Records
{
    public class DirectoryRecord
    {
        public long indexOffset;
        public ulong Hash;
        public int Offset;
        public int Size;
        public uint Unknown;
        public List<string> paths;

        public DirectoryRecord(System.IO.BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            Hash = br.ReadUInt64();
            Offset = br.ReadInt32();
            Size = br.ReadInt32();
            Unknown = br.ReadUInt32();
            paths = new List<string>();
        }
    }
}