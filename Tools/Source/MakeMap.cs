//Matt Hines - Make Map Tool

using System.IO;
using System;

public static class MakeMap
{
	public static void Main(string[] Args)
	{
		try
		{
			string name, creator, tiles, mask, back, mucus;
			int wid = 0, hgt = 0, tilew = 0, tileh = 0;
			
			Console.Write("Please enter the name of the map: ");
			name = Console.ReadLine();
			
			Console.Write("Please enter the creator of the map (leave blank for system user): ");
			creator = Console.ReadLine();
			if (creator == "")
				creator = System.Environment.UserName;
			
			Console.Write("Please enter the name of the tiles image: ");
			tiles = Console.ReadLine();
			
			//Console.Write("Please enter the name of the mask image: ");
			//mask = Console.ReadLine();
			mask = tiles;
			
			Console.Write("Please enter the name of the background image: ");
			back = Console.ReadLine();
			
			Console.Write("Please enter the name of the mucus image: ");
			mucus = Console.ReadLine();
			
			while (wid == 0)
			{
				Console.Write("Please enter the width of the map (positive integer): ");
				int.TryParse(Console.ReadLine(), out wid);
			}
			
			while (hgt == 0)
			{
				Console.Write("Please enter the height of the map (positive integer): ");
				int.TryParse(Console.ReadLine(), out hgt);
			}
			
			while (tilew == 0)
			{
				Console.Write("Please enter the width of a single tile (positive integer): ");
				int.TryParse(Console.ReadLine(), out tilew);
			}
			
			while (tileh == 0)
			{
				Console.Write("Please enter the height of a single tile (positive integer): ");
				int.TryParse(Console.ReadLine(), out tileh);
			}
		
			Console.Write("Please enter the name of the file to write the map to: ");
			string file = Console.ReadLine();
			using (SharpCompress.Archive.Zip.ZipArchive zip = SharpCompress.Archive.Zip.ZipArchive.Create())
			{
				MemoryStream meta = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}\n{1}\n{2} {3}\n{4} {5}\n{6}\n{7}\n{7}\n{8}\n{9}\n", name, creator, wid, hgt,
					tilew, tileh, "Tile", tiles, mask, back, mucus)));
				meta.Seek(0, SeekOrigin.Begin);
				zip.AddEntry("meta", meta, meta.Length, null);
				
				StreamWriter mapStream = new StreamWriter(new MemoryStream((wid * 2) * hgt)) { AutoFlush = true };
                //write map
                for (int y = 0; y < hgt; y++)
                {
                    for (int x = 0; x < wid; x++)
                        mapStream.Write(0 + ",");
                    mapStream.WriteLine();
                }
                mapStream.BaseStream.Seek(0, SeekOrigin.Begin);
                zip.AddEntry("map", mapStream.BaseStream, mapStream.BaseStream.Length, null);
				FileStream stream = new FileStream(file, FileMode.Create);
				zip.SaveTo(stream, SharpCompress.Common.CompressionType.Deflate);
				stream.Close();
				meta.Close();
				mapStream.Close();
				
				Console.WriteLine("\nWrote map to " + file);
			}
		}
		catch (Exception expt)
		{
			Console.WriteLine("\nError writing map\n " + expt.Message);
		}
	}
}