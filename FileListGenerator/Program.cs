using LibBundle;
using System;
using System.IO;

namespace FileListGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args.Length > 0 && Path.GetFileName(args[0]) == "_.index.bin" ? args[0] : "_.index.bin";
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found: " + path);
                Console.WriteLine("Click enter to exit . . .");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Loading . . .");
            var ic = new IndexContainer(path);
            Console.WriteLine("Found:");
            Console.WriteLine(ic.Bundles.Length.ToString() + " BundleRecords");
            Console.WriteLine(ic.Files.Length.ToString() + " FileRecords");
            Console.WriteLine(ic.Directorys.Length.ToString() + " DirectoryRecords");

            Console.WriteLine(Environment.NewLine + "Generating FileList . . .");
            var sw = File.CreateText("FileList.yml");
            foreach (var b in ic.Bundles)
            {
                sw.WriteLine(b.Name + ":");
                foreach (var f in b.Files)
                    sw.WriteLine("- " + f.path);
            }
            sw.Flush();
            sw.Close();
            Console.WriteLine("Done!");

            Console.WriteLine(Environment.NewLine + "Click enter to exit . . .");
            Console.ReadLine();
        }
    }
}