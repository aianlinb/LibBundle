using LibBundle.Records;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibBundle
{
    public class IndexContainer : BundleContainer
    {
        public BundleRecord[] Bundles;
        public FileRecord[] Files;
        public DirectoryRecord[] Directorys;
        public Dictionary<ulong, FileRecord> FindFiles = new Dictionary<ulong, FileRecord>();
        public Dictionary<ulong, string> Hashes;
        public byte[] directoryBundleData;

        public IndexContainer(string path) : base(path)
        {
            var data = Read();
            data.Seek(0, SeekOrigin.Begin);
            var br = new BinaryReader(data);

            int bundleCount = br.ReadInt32();
            Bundles = new BundleRecord[bundleCount];
            for (int i=0; i< bundleCount; i++)
                Bundles[i] = new BundleRecord(br) { bundleIndex = i };
            
            int fileCount = br.ReadInt32();
            Files = new FileRecord[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                var f = new FileRecord(br);
                Files[i] = f;
                FindFiles[f.Hash] = f;
                f.bundleRecord = Bundles[f.BundleIndex];
                Bundles[f.BundleIndex].Files.Add(f);
            }

            int directoryCount = br.ReadInt32();
            Directorys = new DirectoryRecord[directoryCount];
            for (int i = 0; i < directoryCount; i++)
                Directorys[i] = new DirectoryRecord(br);

            var tmp = br.BaseStream.Position;
            directoryBundleData = br.ReadBytes((int)(br.BaseStream.Length - tmp));
            br.BaseStream.Seek(tmp, SeekOrigin.Begin);

            var directoryBundle = new BundleContainer(br);
            var br2 = new BinaryReader(directoryBundle.Read(br));
            Hashes = new Dictionary<ulong, string>(Files.Length);
            foreach (var d in Directorys)
            {
                var temp = new List<string>();
                bool Base = false;
                br2.BaseStream.Seek(d.Offset, SeekOrigin.Begin);
                while (br2.BaseStream.Position - d.Offset <= d.Size - 4)
                {
                    int index = br2.ReadInt32();
                    if (index == 0)
                    {
                        Base = !Base;
                        if (Base)
                            temp = new List<string>();
                    }
                    else
                    {
                        index -= 1;
                        var sb = new StringBuilder();
                        char c;
                        while ((c = br2.ReadChar()) != 0)
                            sb.Append(c);
                        var str = sb.ToString();
                        if (index < temp.Count)
                            str = temp[index] + str;
                        if (Base)
                            temp.Add(str);
                        else
                        {
                            d.paths.Add(str);
                            Hashes[FNV1a64Hash(str)] = str;
                        }
                    }
                }
            }
            br2.Close();
        }

        public static ulong FNV1a64Hash(string str)
        {
            if (str.EndsWith("/"))
            {
                str.TrimEnd(new char[] { '/' });
                str += "++";
            }
            else
                str = str.ToLower() + "++";

            var bs = Encoding.UTF8.GetBytes(str);
            ulong hash = 0xcbf29ce484222325;
            foreach (var by in bs)
                hash = (hash ^ by) * 0x100000001b3;

            return hash;
        }

        public override void Save(string path)
        {
            var bw = new BinaryWriter(new MemoryStream());
            bw.Write(Bundles.Length);
            foreach (var b in Bundles)
            {
                bw.Write(b.nameLength);
                bw.Write(Encoding.UTF8.GetBytes(b.Name), 0, b.nameLength);
                bw.Write(b.UncompressedSize);
            }
            bw.Write(Files.Length);
            foreach (var f in Files)
            {
                bw.Write(f.Hash);
                bw.Write(f.BundleIndex);
                bw.Write(f.Offset);
                bw.Write(f.Size);
            }
            bw.Write(Directorys.Length);
            foreach (var d in Directorys)
            {
                bw.Write(d.Hash);
                bw.Write(d.Offset);
                bw.Write(d.Size);
                bw.Write(d.RecursiveSize);
            }
            bw.Write(directoryBundleData);
            bw.Flush();

            dataToSave = ((MemoryStream)bw.BaseStream).ToArray();
            bw.Close();
            base.Save(path);
        }
    }
}