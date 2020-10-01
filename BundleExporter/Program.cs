using System;
using System.IO;
using System.Linq;

namespace BundleExporter
{
    class Program
    {
        static void Main()
        {
            if (!File.Exists("_.index.bin"))
            {
                Console.WriteLine("File not found: _.index.bin");
                Console.WriteLine("Click enter to exit . . .");
                Console.ReadLine();
                return;
            }
            if (!File.Exists("LibBundle.dll"))
            {
                Console.WriteLine("File not found: oo2core_8_win64.dll");
                Console.WriteLine("Click enter to exit . . .");
                Console.ReadLine();
                return;
            }
            if (!File.Exists("oo2core_8_win64.dll"))
            {
                Console.WriteLine("File not found: oo2core_8_win64.dll");
                Console.WriteLine("Click enter to exit . . .");
                Console.ReadLine();
                return;
            }

            var ic = new LibBundle.IndexContainer("_.index.bin");
            Console.WriteLine("Found:");
            Console.WriteLine(ic.Bundles.Length.ToString() + " BundleRecords");
            Console.WriteLine(ic.Files.Length.ToString() + " FileRecords");
            Console.WriteLine(ic.Directorys.Length.ToString() + " DirectoryRecords");
            var ExistBundle = ic.Bundles.Where(o => File.Exists(o.Name));
            Console.WriteLine(ExistBundle.Count().ToString() + " bundle.bin");
            Console.WriteLine();

            int count = 0;
            foreach (var b in ExistBundle)
                count += b.Files.Count;
            Console.Write("Exporting files . . . (");
            var str = "/" + count.ToString() + ")";
            count = 0;
            foreach (var b in ExistBundle)
            {
                var data = b.Bundle.Read();
                foreach (var f in b.Files)
                {
                    count++;
                    Console.CursorLeft = 23;
                    Console.Write(count);
                    Console.Write(str);
                    try
                    {
                        var path = ic.Hashes[f.Hash];
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        var by = new byte[f.Size];
                        data.Position = f.Offset;
                        data.Read(by, 0, f.Size);
                        File.WriteAllBytes(path, by);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
                data.Close();
            }
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("Click enter to exit . . .");
            Console.ReadLine();
        }
    }
}