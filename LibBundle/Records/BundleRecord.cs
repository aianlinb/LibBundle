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
        public int Size;
        public List<FileRecord> Files;
        internal Dictionary<FileRecord, byte[]> dataToAdd = new Dictionary<FileRecord, byte[]>();
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
            Size = br.ReadInt32();
            Files = new List<FileRecord>();
        }

        public void Read()
        {
            _bundle = new BundleContainer(Name);
        }

        public void Save(string path)
        {
            var data = Bundle.Read();
            var dataToSave = new MemoryStream();
            foreach (var f in Files)
            {
                if (dataToAdd.ContainsKey(f))
                {
                    f.Offset = (int)dataToSave.Position;
                    dataToSave.Write(dataToAdd[f], 0, f.Size);
                }
                else
                {
                    var b = new byte[f.Size];
                    data.Seek(f.Offset, SeekOrigin.Begin);
                    data.Read(b, 0, f.Size);
                    f.Offset = (int)dataToSave.Position;
                    dataToSave.Write(b, 0, f.Size);
                }
            }
            Bundle.dataToSave = dataToSave.ToArray();
            Bundle.Save(path);
            dataToAdd = new Dictionary<FileRecord, byte[]>();
            data.Close();
            dataToSave.Close();
        }
    }
}