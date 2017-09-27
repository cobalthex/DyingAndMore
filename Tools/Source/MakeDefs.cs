using System;
using System.IO;
using System.Reflection;
using Takai.Data;

static class Program
{
	public static void Main(string[] args)
	{
		if (args.Length < 1)
		throw new ArgumentException("Format: <Library> [Type...]");

		var asm = Assembly.LoadFrom(args[0]);
		Serializer.LoadTypesFrom(asm);

		using (var writer = new StreamWriter(Console.OpenStandardOutput()))
		{
			//list defs
			if (args.Length == 1)
			{
				foreach (var rt in Serializer.RegisteredTypes)
				{
					if (rt.Value.Assembly == asm)
					{
						writer.Write(rt.Key + " ");
						if (rt.Value.IsEnum)
							writer.Write("(Enum) ");

						Serializer.TextSerialize(writer, Serializer.DescribeType(rt.Value));
						writer.Write("\n");
					}
				}
			}

			else
			{
				for (int i = 1; i < args.Length; ++i)
				{
					if (i > 1)
						writer.WriteLine();
					Serializer.TextSerialize(writer, Activator.CreateInstance(Serializer.RegisteredTypes[args[i]]));
				}
			}
		}
	}
}