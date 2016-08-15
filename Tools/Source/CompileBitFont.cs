//compile bitmap font v1 -- Dejitaru Forge 2012

using System;
using System.Linq;
using System.IO;
using System.IO.Compression;

public static class CompileBitFont
{	
	public static void Main(string[] Args)
	{
		if (Args.Length < 3)
		{
		 
			Console.WriteLine("CompileBitFont <Input metadata> <Input image> <Output font>");
			Environment.ExitCode = 1;
			return;
		}
	
		var lines = File.ReadAllLines(Args[0]).Where(l => !String.IsNullOrWhiteSpace(l)).ToArray();
		using (var stream = new DeflateStream(new FileStream(Args[2], FileMode.Create), CompressionMode.Compress, false))
		{
			stream.Write(BitConverter.GetBytes(lines.Length), 0, 4);
			foreach (var line in lines)
			{
				var split = line.Substring(2).Split(' ');
				
				stream.Write(BitConverter.GetBytes(line[0]), 0, 2); //codepoint
				stream.Write(BitConverter.GetBytes(Int32.Parse(split[0])), 0, 4); //x
				stream.Write(BitConverter.GetBytes(Int32.Parse(split[1])), 0, 4); //y
				stream.Write(BitConverter.GetBytes(Int32.Parse(split[2])), 0, 4); //width
				stream.Write(BitConverter.GetBytes(Int32.Parse(split[3])), 0, 4); //height
			}

			var img = new FileInfo(Args[1]);
			stream.Write(BitConverter.GetBytes(img.Length), 0, 8);
			
			using (var istream = img.OpenRead())
				istream.CopyTo(stream);
		}
	}
}