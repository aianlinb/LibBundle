using System.Collections.Generic;
using System.IO;

namespace LibBundle.Records
{
    public class BundleRecord
    {
        public long indexOffset;
        public int bundleIndex;
        public int nameLength;
        public string Name;
        public int UncompressedSize;
        public List<FileRecord> Files;
        internal Dictionary<FileRecord, byte[]> FileToAdd = new Dictionary<FileRecord, byte[]>();
        private BundleContainer _bundle;

        public BundleContainer Bundle
        {
            get
            {
                if (_bundle == null)
                    Read();
                return _bundle;
            }
        }

        public BundleRecord(BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            nameLength = br.ReadInt32();
            Name = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLength)) + ".bundle.bin";
            UncompressedSize = br.ReadInt32();
            Files = new List<FileRecord>();
        }

        public void Read()
        {
            _bundle = new BundleContainer(Name);
        }

        public void Save(string path)
        {
            var data = Bundle.Read();
            foreach (var d in FileToAdd)
            {
                d.Key.Offset = (int)data.Position;
                data.Write(d.Value, 0, d.Key.Size);
            }
            UncompressedSize = (int)data.Length;
            FileToAdd = new Dictionary<FileRecord, byte[]>();
            data.Position = 0;
            Bundle.Save(data, path);
            data.Close();
        }
        public byte[] Save()
        {
            using var data = Bundle.Read();
            foreach (var d in FileToAdd)
            {
                d.Key.Offset = (int)data.Position;
                data.Write(d.Value, 0, d.Key.Size);
            }
            UncompressedSize = (int)data.Length;
            FileToAdd = new Dictionary<FileRecord, byte[]>();
            data.Position = 0;
            return Bundle.Save(data);
        }
    }
}