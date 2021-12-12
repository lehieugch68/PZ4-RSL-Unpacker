using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PZ4_RSL_Unpacker
{
    public static class RSL
    {
        private struct Header
        {
            public int Magic;
            public int Count;
            public int HeaderDataOffset;
            public int Unk;
            public int TrailerOffset;
            public byte[] Trailer;
        }
        private struct RMHGEntry
        {
            public int Offset;
            public int Size;
            public byte[] UnkBytes; //24
            public byte[] Data;
            public string FileName;
        }
        private static Header ReadHeader(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Header header = new Header();
            header.Magic = reader.ReadInt32();
            if (header.Magic != 0x47484D52) throw new Exception("Unsupported file type.");
            header.Count = reader.ReadInt32();
            header.HeaderDataOffset= reader.ReadInt32();
            header.Unk = reader.ReadInt32();
            header.TrailerOffset = reader.ReadInt32();
            
            return header;
        }
        private static Dictionary<string, string> RSLExtension = new Dictionary<string, string>
        {
            { "GDLG", ".GDLG" },
            { "RMHG", ".RSL" }
        };
        private static RMHGEntry[] ReadEntries(ref BinaryReader reader, ref Header header)
        {
            List<RMHGEntry> result = new List<RMHGEntry>();
            reader.BaseStream.Seek(header.HeaderDataOffset, SeekOrigin.Begin);
            for (int i = 0; i < header.Count; i++)
            {
                RMHGEntry entry = new RMHGEntry();
                entry.Offset = reader.ReadInt32();
                entry.Size = reader.ReadInt32();
                entry.UnkBytes = reader.ReadBytes(0x18);
                long temp = reader.BaseStream.Position;
                reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                entry.Data = reader.ReadBytes(entry.Size);
                reader.BaseStream.Seek(temp, SeekOrigin.Begin);
                string magic = Encoding.ASCII.GetString(entry.Data.Take(4).ToArray());
                string ext = string.Empty;
                RSLExtension.TryGetValue(magic, out ext);
                entry.FileName = $"{i}{ext}";
                result.Add(entry);
            }
            if (header.TrailerOffset > 0)
            {
                reader.BaseStream.Seek(header.TrailerOffset, SeekOrigin.Begin);
                header.Trailer = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }
            return result.ToArray();
        }
        public static void Unpack(string file, string des)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(file));
            Header header = ReadHeader(ref reader);
            RMHGEntry[] entries = ReadEntries(ref reader, ref header);
            for (int i = 0; i < entries.Length; i++)
            {
                string filePath = Path.Combine(des, entries[i].FileName);
                if (!Directory.Exists(des)) Directory.CreateDirectory(des);
                File.WriteAllBytes(filePath, entries[i].Data);
            }
            if (header.TrailerOffset > 0) File.WriteAllBytes(Path.Combine(des, "Trailer"), header.Trailer);
            reader.Close();
        }
        public static byte[] Repack(string file, string dir)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(file));
            Header header = ReadHeader(ref reader);
            RMHGEntry[] entries = ReadEntries(ref reader, ref header);
            MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                writer.Write(reader.ReadBytes(header.HeaderDataOffset));
                writer.Write(new byte[header.Count * 0x20]);
                for (int i = 0; i < entries.Length; i++)
                {
                    entries[i].Offset = (int)writer.BaseStream.Position;
                    string filePath = Path.Combine(dir, entries[i].FileName);
                    if (File.Exists(filePath))
                    {
                        entries[i].Data = File.ReadAllBytes(filePath);
                        entries[i].Size = entries[i].Data.Length;
                    }
                    if (entries[i].Data.Length % 0x20 != 0)
                    {
                        byte[] bytes = new byte[entries[i].Data.Length + (0x20 - (entries[i].Data.Length % 0x20))];
                        entries[i].Data.CopyTo(bytes, 0);
                        entries[i].Data = bytes;
                    }
                    writer.Write(entries[i].Data);
                    long temp = writer.BaseStream.Position;
                    writer.BaseStream.Seek(header.HeaderDataOffset + (0x20 * i), SeekOrigin.Begin);
                    writer.Write(entries[i].Offset);
                    writer.Write(entries[i].Size);
                    writer.Write(entries[i].UnkBytes);
                    writer.BaseStream.Position = temp;
                }
                if (header.TrailerOffset > 0)
                {
                    header.TrailerOffset = (int)writer.BaseStream.Position;
                    if (File.Exists(Path.Combine(dir, "Trailer"))) header.Trailer = File.ReadAllBytes(Path.Combine(dir, "Trailer"));
                    writer.Write(header.Trailer);
                    writer.BaseStream.Seek(0x10, SeekOrigin.Begin);
                    writer.Write(header.TrailerOffset);
                }
                
            }
            reader.Close();
            return stream.ToArray();
        }
    }
}
