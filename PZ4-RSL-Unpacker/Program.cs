using System;
using System.IO;

namespace PZ4_RSL_Unpacker
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Title = "PZ4 RSL Unpacker by LeHieu - VietHoaGame";
            if (args.Length > 0)
            {
                foreach (string file in args)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    FileAttributes attr = File.GetAttributes(file);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        string rsl = Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.RSL");
                        byte[] result = RSL.Repack(rsl, $"{Path.Combine(Path.GetFileNameWithoutExtension(file))}");
                        //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), $"{rsl}.new"), result);
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), $"{rsl}"), result);
                    }
                    else if (ext == ".rsl")
                    {
                        RSL.Unpack(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                    }
                    else if (ext == ".gdlg")
                    {
                        GDLG.Unpack(file, Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.txt"));
                    }
                    else if (ext == ".txt")
                    {
                        byte[] result = GDLG.Repack(file);
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}-new.GDLG"), result);
                    }
                }
            }
            else
            {
                Console.WriteLine("Please drag and drop files/folder into this tool to unpack/repack.");
            }
            Console.ReadKey();
        }
    }
}
