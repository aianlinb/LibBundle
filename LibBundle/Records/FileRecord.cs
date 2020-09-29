using System.IO;

namespace LibBundle.Records
{
    public class FileRecord
    {
        public long indexOffset;
        public ulong Hash;
        public int BundleIndex;
        public int Offset;
        public int Size;
        public BundleRecord bundleRecord;

        public FileRecord(BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            Hash = br.ReadUInt64();
            BundleIndex = br.ReadInt32();
            Offset = br.ReadInt32();
            Size = br.ReadInt32();
        }

        public byte[] Read(Stream stream = null)
        {
            if (!bundleRecord.dataToAdd.TryGetValue(this, out byte[] b))
            {
                b = new byte[Size];
                var data = stream == null ? bundleRecord.Bundle.Read() : stream;
                data.Seek(Offset, SeekOrigin.Begin);
                data.Read(b, 0, Size);
            }
            return b;
        }

        public void Move(BundleRecord target)
        {
            if (bundleRecord.dataToAdd.TryGetValue(this, out byte[] data))
                bundleRecord.dataToAdd.Remove(this);
            else
                data = Read();
            bundleRecord.Files.Remove(this);
            target.Files.Add(this);
            target.dataToAdd[this] = data;
            bundleRecord = target;
            BundleIndex = target.bundleIndex;
        }

        public void Write(byte[] data)
        {
            Size = data.Length;
            bundleRecord.dataToAdd[this] = data;
        }
    }
}