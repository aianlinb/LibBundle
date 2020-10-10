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
            UncompressedSize = br.ReadInt32();
            Files = new List<FileRecord>();
        }

        public void Read()
        {
            _bundle = new BundleContainer(Name);
        }

        public void Save(string path)
        {
            var odata = Bundle.Read();
            var dataToSave = new MemoryStream();
            foreach (var d in dataToAdd)
            {
                d.Key.Offset = (int)(odata.Length + dataToSave.Position);
                dataToSave.Write(d.Value, 0, d.Key.Size);
            }
            dataToSave.Position = 0;
            dataToSave.CopyTo(odata);
            Bundle.dataToSave = odata.ToArray();
            UncompressedSize = Bundle.dataToSave.Length;
            Bundle.Save(path);
            dataToAdd = new Dictionary<FileRecord, byte[]>();
            odata.Close();
            dataToSave.Close();
        }
    }
}