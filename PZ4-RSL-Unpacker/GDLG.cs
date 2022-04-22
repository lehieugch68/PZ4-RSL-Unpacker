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
                        sw.WriteLine($"#TITLE={entries[pages[i].Title].Text}");
                        sw.WriteLine($"#TITLE_INDEX={pages[i].Title}\n");
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
		public static byte[] Repack(string txt)
        {
			string gdlg = Path.Combine(Path.GetDirectoryName(txt), $"{Path.GetFileNameWithoutExtension(txt)}.GDLG");
			BinaryReader reader = new BinaryReader(File.OpenRead(gdlg));
			Header header = ReadHeader(ref reader);
			TextEntry[] entries = ReadEntries(ref reader, header);
            Page[] pages = ReadPages(ref reader, header);
            List<string> strs = new List<string>();
            int index = 0;
            using (StreamReader sr = new StreamReader(txt))
            {
                while (!sr.EndOfStream)
                {
                    string line = string.Empty;
                    while (!line.StartsWith("#PAGE=") && !sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                    }
                    if (sr.EndOfStream) break;
                    int pageIndex = int.Parse(line.Split('=')[1]);
                    string title = sr.ReadLine().Split('=')[1].Trim();
                    int titleIndex = int.Parse(sr.ReadLine().Split('=')[1]);
                    strs.Add(title);
                    pages[pageIndex].Title = index++;
                    if (entries[titleIndex + 1].Text == "NON")
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
                        line = sr.ReadLine();
                        while (!line.StartsWith("*/"))
                        {
                            strs.Add(line);
                            count++;
                            line = sr.ReadLine();
                        }
                        pages[pageIndex].PageTables[tableIndex].Lines = new LineIndex[count];
                        pages[pageIndex].PageTables[tableIndex].LineCount = (ushort)count;
                        Console.WriteLine(count);
                        for (int x = 0; x < count; x++)
                        {
                            pages[pageIndex].PageTables[i].Lines[x].Index = index++;
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
                reader.BaseStream.Position = writer.BaseStream.Position;
                writer.Write(reader.ReadBytes(0x14));
                writer.Write(new byte[header.PageCount * 4]);
                if ((header.PageCount * 4) % 0x20 != 0) writer.Write(new byte[0x20 - ((header.PageCount * 4) % 0x20)]);
                header.DialogTableOffset = (int)writer.BaseStream.Position;
                writer.Write(new byte[header.LineCount * 4]);
                if ((header.LineCount * 4) % 0x10 != 0) writer.Write(new byte[0x10 - ((header.LineCount * 4) % 0x10)]);
                header.PageDataOffset = (int)writer.BaseStream.Position;
                writer.BaseStream.Position = 0xC;
                writer.Write(header.PageDataOffset);
                writer.BaseStream.Position = header.PageDataOffset;
                long pagePointer = 0;
                for (int i = 0; i < header.PageCount; i++)
                {
                    pages[i].Pointer = (int)pagePointer;
                    long pageOffset = writer.BaseStream.Position;
                    writer.BaseStream.Position = header.PageTableOffset + (i * 4);
                    writer.Write(pages[i].Pointer);
                    writer.BaseStream.Position = pageOffset;
                    writer.Write(pages[i].Title);
                    writer.Write(pages[i].TableCount);
                    writer.Write(pages[i].TableOffset);
                    writer.Write(new byte[0x14]);
                    long tablePointerOffset = writer.BaseStream.Position;
                    writer.Write(new byte[pages[i].TableCount * 4]);
                    if ((pages[i].TableCount * 4) % 0x10 != 0) writer.Write(new byte[(0x10 - ((pages[i].TableCount * 4) % 0x10))]);
                    for (int x = 0; x < pages[i].TableCount; x++)
                    {
                        long tableOffset = writer.BaseStream.Position;
                        writer.BaseStream.Position = tablePointerOffset + (x * 4);
                        writer.Write((int)(tableOffset - pageOffset));
                        writer.BaseStream.Position = tableOffset;
                        writer.Write(pages[i].PageTables[x].Magic);
                        writer.Write(pages[i].PageTables[x].LineCount);
                        writer.Write(pages[i].PageTables[x].Unk);
                        long linePointerOffset = writer.BaseStream.Position;
                        writer.Write(new byte[pages[i].PageTables[x].LineCount * 4]);
                        if ((pages[i].PageTables[x].LineCount * 4) % 0x10 != 0) writer.Write(new byte[0x10 - ((pages[i].PageTables[x].LineCount * 4) % 0x10)]);
                        for (int y = 0; y < pages[i].PageTables[x].LineCount; y++)
                        {
                            long lineOffset = writer.BaseStream.Position;
                            writer.BaseStream.Position = linePointerOffset + (y * 4);
                            writer.Write((int)(lineOffset - tableOffset));
                            writer.BaseStream.Position = lineOffset;
                            writer.Write(pages[i].PageTables[x].Lines[y].Index);
                            writer.Write(new byte[0xC]);
                        }
                    }
                    pagePointer += writer.BaseStream.Position - pageOffset;
                }
                header.DialogDataOffset = (int)writer.BaseStream.Position;
                writer.BaseStream.Position = 0x14;
                writer.Write(header.DialogDataOffset);
                writer.BaseStream.Position = header.DialogDataOffset;
                for (int i = 0; i < header.LineCount; i++)
                {
                    byte[] encoded = TextEncode(strs[i]);
                    long textPointer = writer.BaseStream.Position;
                    writer.BaseStream.Position = header.DialogTableOffset + (i * 4);
                    writer.Write((int)(textPointer - header.DialogDataOffset));
                    writer.BaseStream.Position = textPointer;
                    writer.Write(encoded);
                }

                if (writer.BaseStream.Length % 0x10 != 0)
                {
                    int padding = (int)(0x10 - (writer.BaseStream.Length % 0x10));
                    writer.Write(new byte[padding]);
                }
            }
            return stream.ToArray();
        }
    }
}
