using LibBundle.Records;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibBundle
{
    public class IndexContainer
    {
        public BundleContainer BundleContainer;
        public BundleRecord[] Bundles;
        public FileRecord[] Files;
        public DirectoryRecord[] Directorys;
        public Dictionary<ulong, FileRecord> FindFiles = new Dictionary<ulong, FileRecord>();
        public HashSet<string> Paths = new HashSet<string>();
        public byte[] directoryBundleData;

        private static BinaryReader tmp;
        public IndexContainer(string path) : this(tmp = new BinaryReader(File.OpenRead(path)))
        {
            tmp.Close();
            tmp = null;
        }
        public IndexContainer(BinaryReader br)
        {
            BundleContainer = new BundleContainer(br);
            var data = BundleContainer.Read(br);
            data.Seek(0, SeekOrigin.Begin);
            var databr = new BinaryReader(data);

            int bundleCount = databr.ReadInt32();
            Bundles = new BundleRecord[bundleCount];
            for (int i = 0; i < bundleCount; i++)
                Bundles[i] = new BundleRecord(databr) { bundleIndex = i };

            int fileCount = databr.ReadInt32();
            Files = new FileRecord[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                var f = new FileRecord(databr);
                Files[i] = f;
                FindFiles[f.Hash] = f;
                f.bundleRecord = Bundles[f.BundleIndex];
                Bundles[f.BundleIndex].Files.Add(f);
            }

            int directoryCount = databr.ReadInt32();
            Directorys = new DirectoryRecord[directoryCount];
            for (int i = 0; i < directoryCount; i++)
                Directorys[i] = new DirectoryRecord(databr);

            var tmp = databr.BaseStream.Position;
            directoryBundleData = databr.ReadBytes((int)(databr.BaseStream.Length - tmp));
            databr.BaseStream.Seek(tmp, SeekOrigin.Begin);

            var directoryBundle = new BundleContainer(databr);
            var br2 = new BinaryReader(directoryBundle.Read(databr));
            // Array.Sort(Directorys, new Comparison<DirectoryRecord>((dr1, dr2) => { return dr1.Offset > dr2.Offset ? 1 : -1; }));
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
                            Paths.Add(str);
                            var f = FindFiles[FNV1a64Hash(str)];
                            f.path = str;
                            d.children.Add(f);
                            f.parent = d;
                        }
                    }
                }
            }
            br2.Close();
        }

        public virtual void Save(string path)
        {
            BundleContainer.offset = null;
            var bw = new BinaryWriter(File.OpenWrite(path));
            Save(bw);
            bw.Flush();
            bw.Close();
        }
        public virtual void Save(BinaryWriter bw)
        {
            var tmp = new BinaryWriter(new MemoryStream());
            tmp.Write(Bundles.Length);
            foreach (var b in Bundles)
            {
                tmp.Write(b.nameLength);
                tmp.Write(Encoding.UTF8.GetBytes(b.Name), 0, b.nameLength);
                tmp.Write(b.UncompressedSize);
            }
            tmp.Write(Files.Length);
            foreach (var f in Files)
            {
                tmp.Write(f.Hash);
                tmp.Write(f.BundleIndex);
                tmp.Write(f.Offset);
                tmp.Write(f.Size);
            }
            tmp.Write(Directorys.Length);
            foreach (var d in Directorys)
            {
                tmp.Write(d.Hash);
                tmp.Write(d.Offset);
                tmp.Write(d.Size);
                tmp.Write(d.RecursiveSize);
            }
            tmp.Write(directoryBundleData);
            tmp.Flush();

            BundleContainer.Save(tmp.BaseStream, bw);
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
    }
}