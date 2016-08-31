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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                //create an empty def and print it to stdout (usually requires piping to file to see)
                if (e.Args.Length > 1 && e.Args[0].Equals("makedef", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var stream = System.Console.OpenStandardOutput())
                    {
                        var writer = new System.IO.StreamWriter(stream);
                        writer.AutoFlush = true;
                        Console.SetOut(writer);

                        for (int i = 1; i < e.Args.Length; i++)
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
                    Current.Shutdown(0);
                }
            }
        }
    }
}
