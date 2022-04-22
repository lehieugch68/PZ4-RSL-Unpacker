using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PZ4_RSL_Unpacker
{
    public static class GDLG
    {
        #region Structure
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
        private struct Page
        {
            public int Pointer;
            public int Title;
            public int TableCount;
            public int TableOffset;
            public PageTable[] PageTables;
        }
        private struct PageTable
        {
            public int Pointer;
            public short Magic;
            public ushort LineCount;
            public byte[] Unk;
            public LineIndex[] Lines;
        }
        private struct LineIndex
        {
            public int Pointer;
            public int Index;
        }
        #endregion
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
            List<TextEntry> list = new List<TextEntry>();
            reader.BaseStream.Seek(header.DialogTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < header.LineCount; i++)
            {
                TextEntry entry = new TextEntry();
                entry.Pointer = reader.ReadInt32();
                list.Add(entry);
            }
            reader.BaseStream.Seek(header.DialogDataOffset, SeekOrigin.Begin);
            TextEntry[]  result = list.ToArray();
            for (int i = 0; i < header.LineCount; i++)
            {
                List<byte> bytes = new List<byte>();
                byte b = reader.ReadByte();
                while (b != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    if (b == 0x8D) break;
                    bytes.Add(b);
                    b = reader.ReadByte();
                }
                result[i].Data = bytes.ToArray();
                result[i].Text = TextDecode(bytes);

            }
            return result;
        }
        private static Page[] ReadPages(ref BinaryReader reader, Header header)
        {
            Page[] result = new Page[header.PageCount];
            reader.BaseStream.Seek(header.PageTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < header.PageCount; i++)
            {
                result[i].Pointer = reader.ReadInt32();
                long nextPage = reader.BaseStream.Position;
                reader.BaseStream.Seek(header.PageDataOffset + result[i].Pointer, SeekOrigin.Begin);
                result[i].Title = reader.ReadInt32();
                result[i].TableCount = reader.ReadInt32();
                result[i].TableOffset = reader.ReadInt32();
                reader.BaseStream.Seek(header.PageDataOffset + result[i].Pointer + result[i].TableOffset, SeekOrigin.Begin);
                result[i].PageTables = new PageTable[result[i].TableCount];
                for (int x = 0; x < result[i].TableCount; x++)
                {
                    result[i].PageTables[x].Pointer = reader.ReadInt32();
                    long nextTable = reader.BaseStream.Position;
                    reader.BaseStream.Seek(header.PageDataOffset + result[i].Pointer + result[i].PageTables[x].Pointer, SeekOrigin.Begin);
                    long tableStart = reader.BaseStream.Position;
                    result[i].PageTables[x].Magic = reader.ReadInt16();
                    result[i].PageTables[x].LineCount = reader.ReadUInt16();
                    result[i].PageTables[x].Unk = reader.ReadBytes(0x1C);
                    result[i].PageTables[x].Lines = new LineIndex[result[i].PageTables[x].LineCount];
                    for (int y = 0; y < result[i].PageTables[x].LineCount; y++)
                    {
                        result[i].PageTables[x].Lines[y].Pointer = reader.ReadInt32();
                        long nextPointer = reader.BaseStream.Position;
                        reader.BaseStream.Seek(tableStart + result[i].PageTables[x].Lines[y].Pointer, SeekOrigin.Begin);
                        result[i].PageTables[x].Lines[y].Index = reader.ReadInt32();
                        reader.BaseStream.Position = nextPointer;
                    }
                    reader.BaseStream.Position = nextTable;
                }
                reader.BaseStream.Position = nextPage;
            }
            return result;
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
		public static void Unpack(string gdlg, string txt)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(gdlg));
            Header header = ReadHeader(ref reader);
            TextEntry[] entries = ReadEntries(ref reader, header);
            Page[] pages = ReadPages(ref reader, header);
            if (File.Exists(txt)) File.WriteAllText(txt, string.Empty);
            using (FileStream stream = File.Open(txt, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    for (int i = 0; i < pages.Length; i++)
                    {
                        sw.WriteLine($"#PAGE={i}");
                        sw.WriteLine($"#TITLE={entries[pages[i].Title].Text}\n");
                        for (int x = 0; x < pages[i].PageTables.Length; x++)
                        {
                            sw.WriteLine($"/*INDEX={x}");
                            foreach (var line in pages[i].PageTables[x].Lines)
                            {
                                sw.WriteLine(entries[line.Index].Text);
                            }
                            sw.WriteLine($"*/\n");
                        }
                        sw.WriteLine($"#END\n");
                    }
                }
            }
            reader.Close();
        }
		public static void Repack(string txt)
        {
			string gdlg = Path.Combine(Path.GetDirectoryName(txt), $"{Path.GetFileNameWithoutExtension(txt)}.GDLG");
			BinaryReader reader = new BinaryReader(File.OpenRead(gdlg));
			Header header = ReadHeader(ref reader);
			TextEntry[] entries = ReadEntries(ref reader, header);
            Page[] pages = ReadPages(ref reader, header);
            List<string> strs = new List<string>();
            using (StreamReader sr = new StreamReader(txt))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    int index = -1;
                    while (!line.StartsWith("#PAGE="))
                    {
                        line = sr.ReadLine();
                    }
                    int pageIndex = int.Parse(line.Split('=')[1]);
                    string title = sr.ReadLine().Split('=')[1].Trim();
                    strs.Add(title);
                    pages[pageIndex].Title = ++index;
                    if (title == "START")
                    {
                        strs.Add("NON");
                        index++;
                    }
                    for (int i = 0; i < pages[pageIndex].TableCount; i++)
                    {
                        while (!line.StartsWith("/*INDEX="))
                        {
                            line = sr.ReadLine();
                        }
                        int count = 0;
                        int tableIndex = int.Parse(line.Split('=')[1]);
                        while (!line.StartsWith("*/"))
                        {
                            strs.Add(line);
                            count++;
                            line = sr.ReadLine();
                        }
                        pages[pageIndex].PageTables[tableIndex].Lines = new LineIndex[count];
                        pages[pageIndex].PageTables[tableIndex].LineCount = (ushort)count;
                        for (int x = 0; x < count; x++)
                        {
                            pages[pageIndex].PageTables[i].Lines[x].Index = ++index;
                        }
                    }
                    while (!line.StartsWith("#END"))
                    {
                        line = sr.ReadLine();
                    }
                }
            }
            header.LineCount = (short)strs.Count;
            MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(header.Magic);
                writer.Write(header.Unk);
                writer.Write(header.PageCount);
                writer.Write(header.LineCount);

            }
            /*MemoryStream stream = new MemoryStream();
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
            }*/
                //return stream.ToArray();
        }
    }
}
