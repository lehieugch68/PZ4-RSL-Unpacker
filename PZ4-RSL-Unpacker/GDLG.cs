using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PZ4_RSL_Unpacker
{
    public static class GDLG
    {
        private struct Header
        {
            public int Magic;
            public int Unk;
            public short PageCount;
            public short LineCount;
            public int PageDataOffset;
            public int PageTableOffset;
            public int DialogDataOffset;
            public int DialogTableOffset;
        }
        private struct TextEntry
        {
            public int Pointer;
            public byte[] Data;
            public string Text;
        }
        private static Header ReadHeader(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Header header = new Header();
            header.Magic = reader.ReadInt32();
            if (header.Magic != 0x474C4447) throw new Exception("Unsupported file type.");
            header.Unk = reader.ReadInt32();
            header.PageCount = reader.ReadInt16();
            header.LineCount = reader.ReadInt16();
            header.PageDataOffset = reader.ReadInt32();
            header.PageTableOffset = reader.ReadInt32();
            header.DialogDataOffset = reader.ReadInt32();
            header.DialogTableOffset = reader.ReadInt32();
            return header;
        }
        private static TextEntry[] ReadEntries(ref BinaryReader reader, Header header)
        {
            List<TextEntry> result = new List<TextEntry>();
            reader.BaseStream.Seek(header.DialogTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < header.LineCount; i++)
            {
                TextEntry entry = new TextEntry();
                entry.Pointer = reader.ReadInt32();
                result.Add(entry);
            }
            return result.ToArray();
        }
		private static byte[] TextEncode(string source)
		{
			List<byte> list = Encoding.GetEncoding("shift_jis").GetBytes(source).ToList();
			list.Add(0);
			for (int i = 0; i <= list.Count - 1; i++)
			{
				list[i] ^= 141;
			}
			return list.ToArray();
		}
		private static string TextDecode(List<byte> sourceBytes)
		{
			List<byte> list = new List<byte>();
			foreach (byte b in sourceBytes)
			{
				list.Add((byte)(b ^ 141));
			}
            string decoded = Encoding.GetEncoding("shift_jis").GetString(list.ToArray());
			return decoded;
		}
		public static string[] Unpack(string file)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(file));
            Header header = ReadHeader(ref reader);
            TextEntry[] entries = ReadEntries(ref reader, header);
			string[] result = new string[header.LineCount];
            reader.BaseStream.Seek(header.DialogDataOffset, SeekOrigin.Begin);
            for (int i = 0; i < result.Length; i++)
            {
                List<byte> bytes = new List<byte>();
                byte b = reader.ReadByte();
                while (b != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    if (b == 0x8D) break;
                    bytes.Add(b);
                    b = reader.ReadByte();
                }
                entries[i].Data = bytes.ToArray();
                result[i] = TextDecode(bytes);

            }
            reader.Close();
			return result;
        }
		public static byte[] Repack(string file)
        {
			string gdlg = Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.GDLG");
			string[] input = File.ReadAllLines(file);
			BinaryReader reader = new BinaryReader(File.OpenRead(gdlg));
			Header header = ReadHeader(ref reader);
			TextEntry[] entries = ReadEntries(ref reader, header);
			MemoryStream stream = new MemoryStream();
			using (BinaryWriter writer = new BinaryWriter(stream))
            {
				reader.BaseStream.Seek(0, SeekOrigin.Begin);
				writer.Write(reader.ReadBytes(header.DialogDataOffset));
				int pointer = 0;
				for (int i = 0; i < entries.Length; i++)
                {
					if (!input[i].StartsWith("{Copy}")) entries[i].Data = TextEncode(i < input.Length ? input[i] : "");
                    else
                    {
						reader.BaseStream.Position = header.DialogDataOffset + entries[i].Pointer;
						List<byte> bytes = new List<byte>();
						byte b = reader.ReadByte();
						while (b != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
						{
							bytes.Add(b);
							if (b == 0x8D) break;
							b = reader.ReadByte();
						}
						entries[i].Data = bytes.ToArray();
					} 
					writer.Write(entries[i].Data);
					entries[i].Pointer = pointer;
					pointer += entries[i].Data.Length;
                }
				if (writer.BaseStream.Length % 0x20 != 0)
                {
					int padding = (int)(0x20 - (writer.BaseStream.Length % 0x20));
					writer.Write(new byte[padding]);
                }
				writer.BaseStream.Seek(header.DialogTableOffset, SeekOrigin.Begin);
				foreach (var entry in entries)
                {
					writer.Write(entry.Pointer);
                }
            }
			return stream.ToArray();
		}
    }
}
