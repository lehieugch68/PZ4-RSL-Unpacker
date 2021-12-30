using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic;

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
		private static string Bytes2Hex(byte[] data, bool splitBytes = false)
		{
			string text = "";
			int num = 0;
			checked
			{
				int num2 = data.Length - 1;
				for (int i = num; i <= num2; i++)
				{
					if (i > 0 && splitBytes)
					{
						text += " ";
					}
					text += Byte2Hex(data[i]);
				}
				return text;
			}
		}
		private static string Num2Hex(ulong value)
		{
			short num = 1;
			byte[] array;
			short num2;
			short num3;
			checked
			{
				while (Math.Pow(256.0, (double)num) <= value)
				{
					num += 1;
				}
				array = new byte[(int)(num - 1 + 1)];
				num2 = 0;
				num3 = (short)(num - 1);
			}
			for (short num4 = num2; num4 <= num3; num4 += 1)
			{
				array[(int)num4] = Convert.ToByte(decimal.Remainder(new decimal(value), 256m));
				value = checked((ulong)Math.Round((value - unchecked((ulong)array[(int)num4])) / 256.0));
			}
			return Bytes2Hex(array, false);
		}
		private static string Byte2Hex(byte data)
		{
			string str = "";
			str += Num2Char(checked((long)Math.Round(Math.Floor((double)data / 16.0))));
			return str + Num2Char((long)(data % 16));
		}
		private static string Num2Char(long data)
		{
			checked
			{
				string result;
				if (data < 10L)
				{
					result = Conversions.ToString(Strings.Chr((int)(48L + data)));
				}
				else
				{
					result = Conversions.ToString(Strings.Chr((int)(65L + (data - 10L))));
				}
				return result;
			}
		}
		private static byte[] FullEncode(string source)
		{
			List<byte> list = Unicode2SJIS(source);
			list.Add(0);
			int num = 0;
			checked
			{
				int num2 = list.Count - 1;
				for (int i = num; i <= num2; i++)
				{
					list[i] ^= 141;
				}
				return list.ToArray();
			}
		}
		private static string FullDecode(List<byte> sourceBytes)
		{
			List<byte> list = new List<byte>();
			foreach (byte b in sourceBytes)
			{
				list.Add((byte)(b ^ 141));
				Console.Write((b ^ 141).ToString("X") + " ");
				
			}
			Console.WriteLine();
			return SJIS2Unicode(list);
		}
		private static string SJIS2Unicode(List<byte> sourceBytes)
		{
			Encoding encoding = Encoding.GetEncoding("shift_jis");
			string text = encoding.GetString(sourceBytes.ToArray());
			/*Decoder decoder = encoding.GetDecoder();
			int charCount = decoder.GetCharCount(sourceBytes.ToArray(), 0, sourceBytes.Count, true);
			char[] array = new char[checked(charCount - 1 + 1)];
			decoder.GetChars(sourceBytes.ToArray(), 0, sourceBytes.Count, array, 0, true);
			string text = "";
			foreach (char c in array)
			{
				text += c.ToString();
			}*/
			return text;
		}
		private static List<byte> Unicode2SJIS(string source)
		{
			Encoding encoding = Encoding.GetEncoding("shift_jis");
			/*Encoder encoder = encoding.GetEncoder();
			int byteCount = encoder.GetByteCount(Conversions.ToCharArrayRankOne(source), 0, source.Count<char>(), true);
			byte[] array = new byte[checked(byteCount - 1 + 1)];
			encoder.GetBytes(source.ToCharArray(), 0, source.Count<char>(), array, 0, true);*/
			byte[] array = encoding.GetBytes(source);
			return array.ToList<byte>();
		}
		public static string[] Unpack(string file)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(file));
            Header header = ReadHeader(ref reader);
            TextEntry[] entries = ReadEntries(ref reader, header);
			string[] result = new string[header.LineCount];
            reader.BaseStream.Seek(header.DialogDataOffset, SeekOrigin.Begin);
			List<byte> textData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position)).ToList();
			textData.Reverse();
			while (textData.First() == 0)
            {
				textData.RemoveAt(0);
            }
			textData.Reverse();
			int index = 0;
			for (int i = 0; i < result.Length; i++)
            {
				List<byte> bytes = new List<byte>();
				byte b = textData.ElementAt(index);
				while (index < textData.Count)
                {
					index++;
					if (b == 0x8D) break;
					bytes.Add(b);
					b = textData.ElementAt(index);
                }
				entries[i].Data = bytes.ToArray();
				result[i] = FullDecode(bytes);
            }
			reader.Close();
			result = result.ToArray();
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
					if (!input[i].StartsWith("{Copy}")) entries[i].Data = FullEncode(i < input.Length ? input[i] : "");
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
