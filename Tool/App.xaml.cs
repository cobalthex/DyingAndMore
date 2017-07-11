using System;
using System.Windows;

namespace Tool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Takai.Data.Serializer.LoadTypesFrom(typeof(DyingAndMore.DyingAndMoreGame).Assembly);
        }

        void PrintType(Type Type, System.IO.StreamWriter Stream)
        {
            //can use codedom: http://stackoverflow.com/questions/6402864/c-pretty-type-name-function
            
            var l = Type.Name.IndexOf('`');
            Stream.Write(l < 0 ? Type.Name : Type.Name.Substring(0, l));

            if (Type.IsGenericType)
            {
                Stream.Write('<');
                var gp = Type.GetGenericArguments();
                for (var i = 0; i < gp.Length; ++i)
                {
                    if (i > 0)
                        Stream.Write(", ");
                    Stream.Write(gp[i].Name);
                }
                Stream.Write('>');
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                //create an empty def and print it to stdout (usually requires piping to file to see)
                if (e.Args.Length > 1 && e.Args[0].Equals("makedef", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var stream = Console.OpenStandardOutput())
                    {
                        using (var writer = new System.IO.StreamWriter(stream))
                        {
                            Console.SetOut(writer);

                            for (int i = 1; i < e.Args.Length; ++i)
                            {
                                Type ty;
                                if (Takai.Data.Serializer.RegisteredTypes.TryGetValue(e.Args[i], out ty))
                                {
                                    var obj = Activator.CreateInstance(ty);
                                    Takai.Data.Serializer.TextSerialize(writer, obj);
                                    writer.WriteLine();
                                }
                            }
                        }
                        stream.Flush();
                    }
                }

                else if (e.Args[0].Equals("types", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var stream = Console.OpenStandardOutput())
                    {
                        var writer = new System.IO.StreamWriter(stream);
                        writer.AutoFlush = true;
                        Console.SetOut(writer);

                        var enums = new System.Collections.Generic.HashSet<Type>();
                        
                        foreach (var ty in Takai.Data.Serializer.RegisteredTypes)
                        {
                            if (ty.Value.GetCustomAttributes(typeof(Takai.Data.DesignerModdableAttribute), true).Length < 1)
                                continue;

                            PrintType(ty.Value, writer);

                            if (!ty.Value.IsEnum && !ty.Value.IsPrimitive && !typeof(Delegate).IsAssignableFrom(ty.Value))
                            {
                                writer.WriteLine(" {");

                                foreach (var mv in ty.Value.GetProperties())
                                {
                                    if (mv.PropertyType.GetCustomAttributes(typeof(Takai.Data.Serializer.IgnoredAttribute), true).Length > 0 ||
                                        typeof(Delegate).IsAssignableFrom(mv.PropertyType))
                                        continue;

                                    if (mv.PropertyType.IsEnum)
                                    {
                                        enums.Add(mv.PropertyType);
                                        continue;
                                    }

                                    writer.Write("    {0}: ", mv.Name);
                                    PrintType(mv.PropertyType, writer);
                                    writer.WriteLine(';');
                                }

                                foreach (var mv in ty.Value.GetFields())
                                {
                                    if (mv.FieldType.GetCustomAttributes(typeof(Takai.Data.Serializer.IgnoredAttribute), true).Length > 0 ||
                                        typeof(Delegate).IsAssignableFrom(mv.FieldType))
                                        continue;

                                    if (mv.FieldType.IsEnum)
                                    {
                                        enums.Add(mv.FieldType);
                                        continue;
                                    }

                                    writer.Write("    {0}: ", mv.Name);
                                    PrintType(mv.FieldType, writer);
                                    writer.WriteLine(';');
                                }

                                writer.Write('}');
                            }

                            writer.WriteLine(';');
                        }

                        writer.WriteLine("\n\n/* Enums */");

                        foreach (var et in enums)
                        {
                            writer.WriteLine(et.Name + " {");

                            var ety = et.GetEnumUnderlyingType();
                            foreach (var ev in et.GetEnumValues())
                                writer.WriteLine("    {0}: {1};", et.GetEnumName(ev), Convert.ChangeType(ev, ety));

                            writer.WriteLine("};");
                        }
                    }
                }

                Current.Shutdown(0);
            }
        }
    }
}
