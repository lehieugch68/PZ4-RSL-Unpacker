using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

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
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), $"{rsl}.new"), result);
                    }
                    else if (ext == ".rsl")
                    {
                        RSL.Unpack(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                    }
                    else if (ext == ".gdlg")
                    {
                        string[] result = GDLG.Unpack(file);
                        File.WriteAllLines(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.txt"), result);
                    }
                    else if (ext == ".txt")
                    {
                        byte[] result = GDLG.Repack(file);
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}-new.GDLG"), result);
                    }
                    else if (ext == ".bmp")
                    {
                        Bitmap data = new Bitmap(Image.FromFile(file));
                        byte[] result = GCT0Bitmap.WriteI4(data);
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.bitmap"), result);
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
