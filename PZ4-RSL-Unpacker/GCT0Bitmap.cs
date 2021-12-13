using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace PZ4_RSL_Unpacker
{
    public static class GCT0Bitmap
    {
		private static ulong Coords2Offset(int xPos, int yPos, int width, int height, decimal elementLength, int tileWidth = 1, int tileHeight = 1, bool getTileOffset = false, bool trimBorderTiles = true)
		{
			checked
			{
				int num = (int)Math.Round(Math.Ceiling((double)width / (double)tileWidth));
				int num2 = (int)Math.Round(Math.Ceiling((double)height / (double)tileHeight));
				int num3 = num * tileWidth - width;
				int num4 = num3 * tileHeight;
				int num5 = num2 * tileHeight - height;
				int num6 = num5 * tileWidth;
				int num7 = (int)Math.Round(Math.Floor((double)xPos / (double)tileWidth));
				int num8 = (int)Math.Round(Math.Floor((double)yPos / (double)tileHeight));
				int num9 = xPos % tileWidth;
				int num10 = yPos % tileHeight;
				int num11 = tileWidth * tileHeight;
				int num12 = num8 * num + num7;
				int num13 = num10 * tileWidth + num9;
				if (num7 == num - 1)
				{
					num13 -= num10 * num3;
				}
				int num14;
				if (trimBorderTiles)
				{
					num14 = num12 * num11 - num4 * num8;
				}
				else
				{
					num14 = num12 * num11;
				}
				if (num8 == num2 - 1 && trimBorderTiles)
				{
					num14 -= num6 * num7;
				}
				if (getTileOffset)
				{
					return Convert.ToUInt64(decimal.Multiply(new decimal(num14), elementLength));
				}
				return Convert.ToUInt64(decimal.Multiply(new decimal(num14 + num13), elementLength));
			}
		}
		private static byte GetGrey(Color pixel)
		{
			int r = (int)pixel.R;
			int g = (int)pixel.G;
			int b = (int)pixel.B;
			checked
			{
				int num = (int)Math.Round(Math.Ceiling((double)(r + g + b) / 3.0));
				return (byte)num;
			}
		}
		public static Bitmap LoadI4(int imgWidth, int imgHeight, BinaryReader reader)
		{
			checked
			{
				long num = (long)Math.Round(Math.Ceiling((double)(imgWidth * imgHeight) / 2.0));
				byte[] array = reader.ReadBytes((int)num);
				Bitmap result = new Bitmap(imgWidth, imgHeight);
				int num2 = 0;
				int num3 = imgHeight - 1;
				for (int i = num2; i <= num3; i++)
				{
					int num4 = 0;
					int num5 = imgWidth - 1;
					for (int j = num4; j <= num5; j += 8)
					{
						int num6 = Math.Min(8, imgWidth - j);
						int tileHeight = Math.Min(8, imgHeight - i);
						long num7 = (long)Math.Round(Coords2Offset(j, i, imgWidth, imgHeight, 4m, num6, tileHeight, false, true) / 8.0);
						if (num7 < unchecked((long)array.Length))
						{
							int num8 = 0;
							int num9 = num6 - 1;
							for (int k = num8; k <= num9; k += 2)
							{
								byte b = (byte)(array[(int)Math.Round(unchecked((double)num7 + (double)k / 2.0))] & 240);
								Color color = Color.FromArgb((int)b, (int)b, (int)b);
								result.SetPixel(j + k, i, color);
							}
							int num10 = 1;
							int num11 = num6 - 1;
							for (int k = num10; k <= num11; k += 2)
							{
								byte b2 = (byte)((array[(int)Math.Round(unchecked((double)num7 + (double)(checked(k - 1)) / 2.0))] & 15) << 4);
								Color color2 = Color.FromArgb((int)b2, (int)b2, (int)b2);
								result.SetPixel(j + k, i, color2);
							}
						}
					}
				}
				return result;
			}
		}
		public static byte[] WriteI4(Bitmap bitmapData)
		{
			MemoryStream stream = new MemoryStream();
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				checked
				{
					long num = (long)Math.Round((double)((bitmapData.Width + bitmapData.Width % 8) * (bitmapData.Height + bitmapData.Height % 8)) / 2.0);
					byte[] array = new byte[(int)(num - 1L) + 1];
					int num2 = 0;
					int num3 = bitmapData.Width - 1;
					for (int i = num2; i <= num3; i += 8)
					{
						int num4 = 0;
						int num5 = bitmapData.Height - 1;
						for (int j = num4; j <= num5; j++)
						{
							int num6 = Math.Min(8, bitmapData.Width - i);
							int num7 = Math.Min(8, bitmapData.Height - j);
							long num8 = (long)Math.Round(Coords2Offset(i, j, bitmapData.Width, bitmapData.Height, 4m, 8, 8, false, true) / 8.0);
							int num9 = 0;
							int num10 = num6 - 1;
							for (int k = num9; k <= num10; k += 2)
							{
								Color pixel = bitmapData.GetPixel(i + k, j);
								Color pixel2 = bitmapData.GetPixel(i + k + 1, j);
								byte b = GetGrey(pixel);
								byte b2 = GetGrey(pixel2);
								b &= 240;
								b2 = unchecked((byte)((uint)b2 >> 4));
								array[(int)Math.Round(unchecked((double)num8 + (double)k / 2.0))] = unchecked((byte)(b + b2));
							}
						}
					}
					writer.Write(array);
				}
			}
			return stream.ToArray();
		}
	}
}
