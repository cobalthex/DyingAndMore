//compile bitmap font v1 -- Dejitaru Forge 2012

using System;
using System.IO;
using SharpCompress.Compressor.Deflate;

public static class CompileBitFont
{
	public static void cw(string format, params object[] args)
	{
		Console.WriteLine(format, args);
	}
	
	public static void Main(string[] Args)
	{
		try
		{
			string meta = "", font = "";
			cw("Bitmap Font Compiler (v1.0)\n---------------------------");
			while (!System.IO.File.Exists(meta))
			{
				Console.Write("Please enter the font meta data file: ");
				meta = Console.ReadLine();
			}
			while (!System.IO.File.Exists(font))
			{
                Console.Write("Please enter the font image file: ");
				font = Console.ReadLine();
			}
			
			int len = File.ReadAllLines(meta).Length;
			StreamReader metaRead = new StreamReader(new FileStream(meta, FileMode.Open));
			FileStream fontRead = new FileStream(font, FileMode.Open);
			var path = Path.GetFileNameWithoutExtension(font);
			
			var cpz = new DeflateStream(new FileStream(path + ".bfnt", FileMode.Create), SharpCompress.Compressor.CompressionMode.Compress, CompressionLevel.Default, false);
			cpz.Write(BitConverter.GetBytes(len), 0, 4);
			
			while (!metaRead.EndOfStream)
			{
				string[] line = metaRead.ReadLine().Split(new[] {(char)0, ' '});
				int x, y, w, h;
				int.TryParse(line[1], out x);
				int.TryParse(line[2], out y);
				int.TryParse(line[3], out w);
				int.TryParse(line[4], out h);
				cpz.Write(BitConverter.GetBytes(line[0][0]), 0, 2);
				cpz.Write(BitConverter.GetBytes(x), 0, 4);
				cpz.Write(BitConverter.GetBytes(y), 0, 4);
				cpz.Write(BitConverter.GetBytes(w), 0, 4);
				cpz.Write(BitConverter.GetBytes(h), 0, 4);
			}
			cpz.Write(BitConverter.GetBytes(fontRead.Length), 0, 8);
			int sz = 4096;
			byte[] buf = new byte[sz];
			while (fontRead.Position < fontRead.Length)
			{
				int read = fontRead.Read(buf, 0, sz);
				cpz.Write(buf, 0, read);
			}
			cpz.Close();
			metaRead.Close();
			fontRead.Close();
			cw("Write complete. File written to {0}", path + ".bfnt");
			Console.ReadKey();
		}
		catch (Exception expt)
		{
			cw("Error: {0}\n\n{1}", expt.Message, expt.StackTrace);
			Console.ReadKey();
		}
	}
}