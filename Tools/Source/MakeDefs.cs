using System;
using System.IO;
using System.Reflection;
using Takai.Data;

static class Program
{
	public static void Main(string[] args)
	{
		if (args.Length < 2)
			throw new ArgumentException("Format: <Library> <Type> [Type...]");

		var asm = Assembly.LoadFrom(args[0]);
		Serializer.LoadTypesFrom(asm);

		using (var writer = new StreamWriter(Console.OpenStandardOutput()))
		{
			for (int i = 1; i < args.Length; ++i)
			{
				Serializer.TextSerialize(writer, Activator.CreateInstance(Serializer.RegisteredTypes[args[i]]));
				writer.WriteLine();
			}
		}
	}
}