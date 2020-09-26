using LibBundle;
using System;
using System.IO;

namespace FileListGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("_.index.bin"))
            {
                Console.WriteLine("File not found: _.index.bin");
                Console.WriteLine("Click enter to exit . . .");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Loading . . .");
            var gc = new IndexContainer("_.index.bin");
            var fl = File.CreateText("FileList.yml");
            foreach (var b in gc.Bundles)
            {
                fl.WriteLine(b.Name + ":");
                foreach (var f in b.Files)
                    if (gc.Hashes.ContainsKey(f.Hash))
                        fl.WriteLine("- " + gc.Hashes[f.Hash]);
            }
            fl.Flush();
            fl.Close();
            Console.WriteLine("Done!");
            Console.WriteLine("Click enter to exit . . .");
            Console.ReadLine();
        }
    }
}