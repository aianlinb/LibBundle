using System;
using System.IO;

namespace LibBundle
{
    public class BundleContainer
    {
        [System.Runtime.InteropServices.DllImport("oo2core_8_win64.dll")]
        public static extern int OodleLZ_Decompress(byte[] buffer, int bufferSize, byte[] result, long outputBufferSize, int a, int b, int c, IntPtr  d, long e, IntPtr f, IntPtr g, IntPtr h, long i, int ThreadModule);
        [System.Runtime.InteropServices.DllImport("oo2core_8_win64.dll")]
        public static extern int OodleLZ_Compress(ENCODE_TYPES format, byte[] buffer, long bufferSize, byte[] outputBuffer, COMPRESSTION_LEVEL level, IntPtr opts, long offs, long unused, IntPtr scratch, long scratch_size);
        public enum ENCODE_TYPES : uint
        {
            LZH = 0,
            LZHLW = 1,
            LZNIB = 2,
            NONE = 3,
            LZB16 = 4,
            LZBLW = 5,
            LZA = 6,
            LZNA = 7,
            KRAKEN = 8,
            MERMAID = 9,
            BITKNIT = 10,
            SELKIE = 11,
            HYDRA = 12,
            LEVIATHAN = 13
        }

        public enum COMPRESSTION_LEVEL : uint
        {
            None,
            SuperFast,
            VeryFast,
            Fast,
            Normal,
            Optimal1,
            Optimal2,
            Optimal3,
            Optimal4,
            Optimal5
        }

        public string path;
        public long offset = 0;
        public int uncompressed_size;
        public int data_size;
        public int head_size;
        public ENCODE_TYPES encoder;
        public int unknown;
        public long size_decompressed;
        public long size_compressed;
        public int entry_count;
        public int chunk_size;
        public int unknown3;
        public int unknown4;
        public int unknown5;
        public int unknown6;
        internal byte[] dataToSave;

        //For UnPacking
        public BundleContainer(string path)
        {
            this.path = path;
            var br = new BinaryReader(File.OpenRead(path));
            Initialize(br);
            br.Close();
        }

        //For UnPacking
        public BundleContainer(BinaryReader br)
        {
            Initialize(br);
        }

        //For Packing
        public BundleContainer(byte[] data)
        {
            offset = 0;
            size_decompressed = uncompressed_size = data.Length;
            encoder = ENCODE_TYPES.LEVIATHAN;
            entry_count = (data.Length % 262144) == 0 ? data.Length / 262144 : (data.Length / 262144) + 1;
            head_size = entry_count * 4 + 48;
            chunk_size = 262144;
            unknown = 1;
            unknown3 = unknown4 = unknown5 = unknown6 = 0;
            dataToSave = data;
        }
        
        private void Initialize(BinaryReader br)
        {
            offset = br.BaseStream.Position;
            uncompressed_size = br.ReadInt32();
            data_size = br.ReadInt32();
            head_size = br.ReadInt32();
            encoder = (ENCODE_TYPES)br.ReadInt32();
            unknown = br.ReadInt32();
            size_decompressed = (int)br.ReadInt64();
            size_compressed = br.ReadInt64();
            entry_count = br.ReadInt32();
            chunk_size = br.ReadInt32();
            unknown3 = br.ReadInt32();
            unknown4 = br.ReadInt32();
            unknown5 = br.ReadInt32();
            unknown6 = br.ReadInt32();
        }

        //UnPacking
        public MemoryStream Read(BinaryReader br = null)
        {
            if (br == null)
                if (path == null)
                    throw new ArgumentException("BundleContainer implemented using a constructor with BinaryReader parameters must include the br parameter when calling Read()", "br");
                else
                    br = new BinaryReader(File.OpenRead(path));
            br.BaseStream.Seek(offset + 60, SeekOrigin.Begin);

            var chunks = new int[entry_count];
            for (int i = 0; i < entry_count; i++)
            {
                chunks[i] = br.ReadInt32();
            }
            
            var data = new MemoryStream(uncompressed_size);

            for (int i = 0; i < entry_count; i++)
            {
                var b = br.ReadBytes(chunks[i]);
                int size = (i + 1 == entry_count) ? uncompressed_size - (chunk_size * (entry_count - 1)) : chunk_size;
                var toSave = new byte[size + 64];
                OodleLZ_Decompress(b, b.Length, toSave, size, 0, 0, 0, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, 3);
                data.Write(toSave, 0, size);
            }
            br.Close();
            return data;
        }

        //Packing
        public virtual void Save(string path)
        {
            if (dataToSave == null)
                throw new NotSupportedException("Save() only can be called when it's implemented using a constructor with \"byte[] data\" parameters");
            this.path = path;
            size_decompressed = uncompressed_size = dataToSave.Length;
            entry_count = uncompressed_size / chunk_size;
            if (uncompressed_size % chunk_size != 0) entry_count++;
            head_size = entry_count * 4 + 48;
            var bw = new BinaryWriter(File.Create(path));
            bw.BaseStream.Seek(offset + 60 + (entry_count*4), SeekOrigin.Begin);
            data_size = 0;
            var ms = new MemoryStream(dataToSave);
            var chunks = new int[entry_count];
            for (int i = 0; i < entry_count - 1; i++)
            {
                var b = new byte[chunk_size];
                ms.Read(b, 0, chunk_size);
                var by = new byte[b.Length + 548];
                var l = OodleLZ_Compress(ENCODE_TYPES.LEVIATHAN, b, b.Length, by, COMPRESSTION_LEVEL.Normal, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                data_size += chunks[i] = l;
                bw.Write(by, 0, l);
            }
            var b2 = new byte[dataToSave.Length - (entry_count - 1) * chunk_size];
            ms.Read(b2, 0, b2.Length);
            var by2 = new byte[b2.Length + 548];
            var l2 = OodleLZ_Compress(ENCODE_TYPES.LEVIATHAN, b2, b2.Length, by2, COMPRESSTION_LEVEL.Normal, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
            data_size += chunks[entry_count - 1] = l2;
            bw.Write(by2, 0, l2);

            bw.BaseStream.Seek(offset + 60, SeekOrigin.Begin);
            for (int i = 0; i < entry_count; i++)
                bw.Write(chunks[i]);

            size_compressed = data_size;
            bw.BaseStream.Seek(offset, SeekOrigin.Begin);
            bw.Write(uncompressed_size);
            bw.Write(data_size);
            bw.Write(head_size);
            bw.Write((uint) encoder);
            bw.Write(unknown);
            bw.Write(size_decompressed);
            bw.Write(size_compressed);
            bw.Write(entry_count);
            bw.Write(chunk_size);
            bw.Write(unknown3);
            bw.Write(unknown4);
            bw.Write(unknown5);
            bw.Write(unknown6);

            bw.Flush();
            bw.Close();
            ms.Close();
        }
    }
}