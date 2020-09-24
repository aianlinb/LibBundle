using System;
using System.IO;
using System.Linq;

namespace BundleExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("_.index.bin"))
            {
                Console.WriteLine("File not found: _.index.bin");
                Console.WriteLine("Click any key to exit . . .");
                Console.ReadKey();
                return;
            }
            if (!File.Exists("LibBundle.dll"))
            {
                Console.WriteLine("File not found: oo2core_8_win64.dll");
                Console.WriteLine("Click any key to exit . . .");
                Console.ReadKey();
                return;
            }
            if (!File.Exists("oo2core_8_win64.dll"))
            {
                Console.WriteLine("File not found: oo2core_8_win64.dll");
                Console.WriteLine("Click any key to exit . . .");
                Console.ReadKey();
                return;
            }

            var ic = new LibBundle.IndexContainer("_.index.bin");
            Console.WriteLine("Found:");
            Console.WriteLine(ic.Bundles.Length.ToString() + " BundleRecords");
            Console.WriteLine(ic.Directorys.Length.ToString() + " DirectoryRecords");
            Console.WriteLine(ic.Files.Length.ToString() + " FileRecords");
            Console.WriteLine("");

            var ExistFiles = ic.Files.Where(o => File.Exists(o.bundleRecord.Name));
            Console.Write("Exporting files . . . (");
            int count = 0;
            var str = "/" + ExistFiles.Count().ToString() + ")";
            foreach (var f in ExistFiles)
            {
                count++;
                Console.CursorLeft = 23;
                Console.Write(count);
                Console.Write(str);
                try
                {
                    var path = ic.Hashes[f.Hash];
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var fi = File.Create(path);
                    fi.Write(f.Read(), 0, f.Size);
                }
                catch (System.Collections.Generic.KeyNotFoundException) { }
            }
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("Click any key to exit . . .");
            Console.ReadKey();
        }
    }
}