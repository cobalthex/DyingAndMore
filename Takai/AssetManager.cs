using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO.Compression;

namespace Takai
{
    /// <summary>
    /// A simple name based asset managment system that will hold various asset types and manage the lifetime of them
    /// </summary>
    public static class AssetManager
    {
        /// <summary>
        /// The root directory to search for files for this asset manager
        /// </summary>
        public static string RootDirectory { get; set; } = "";

        /// <summary>
        /// The assets themselves
        /// </summary>
        private static Dictionary<string, IDisposable> assets;

        /// <summary>
        /// The graphics device used for this asset manager (for creating texture assets)
        /// </summary>
        public static GraphicsDevice GraphicsDevice { get; set; }

        /// <summary>
        /// The total number of loaded assets in the manager
        /// </summary>
        public static int Count { get { return assets.Count; } }

        static AssetManager()
        {
            assets = new Dictionary<string, IDisposable>();
        }

        /// <summary>
        /// Initialize the asset manager. Required before using
        /// </summary>
        /// <param name="GraphicsDevice">The graphics device to use for texture ops</param>
        /// <param name="RootDirectory">The root directory to search for assets. By default is ''</param>
        public static void Initialize(GraphicsDevice GraphicsDevice, string RootDirectory = "")
        {
            AssetManager.GraphicsDevice = GraphicsDevice;
            AssetManager.RootDirectory = RootDirectory;
            AssetManager.assets = new Dictionary<string, IDisposable>();
        }

        public static Dictionary<string, System.Type> AssetTypes { get; set; } = new Dictionary<string, System.Type>
        {
            { "png", typeof(Texture2D) },
            { "jpg", typeof(Texture2D) },
            { "jpeg", typeof(Texture2D) },
            { "tga", typeof(Texture2D) },
            { "tif", typeof(Texture2D) },
            { "tiff", typeof(Texture2D) },
            { "bmp", typeof(Texture2D) },

            { "bfnt", typeof(Graphics.BitmapFont) },

            { "mp3", typeof(SoundEffect) },
            { "wav", typeof(SoundEffect) },
            { "wma", typeof(SoundEffect) },
            { "ogg", typeof(SoundEffect) },

            { "fx", typeof(Effect) },
            { "mgfx", typeof(Effect) },
        };

        static bool LoadZip(string File)
        {
            using (var stream = new System.IO.FileStream(File, FileMode.Open))
                return LoadZip(stream);
        }

        /// <summary>
        /// Load multiple assets from a package stream, does not load new assets over old ones
        /// </summary>
        /// <param name="Stream">The stream to load from</param>
        /// <param name="IncludeExtensions">Include file extensions in the asset's name</param>
        /// <returns>True on successful load of all assets. False on error loading one or more assets (All assets will be attempted to be loaded)</returns>
        static bool LoadZip(Stream Stream)
        {
            bool result = true;
            using (var zip = new ZipArchive(Stream, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    var type = AssetTypes[Path.GetExtension(entry.FullName)];
                    var file = entry.FullName;

                    if (type != null)
                    {
                        using (var stream = entry.Open())
                        {
                            IDisposable dat = null;

                            if (type == typeof(Texture2D))
                                dat = Load<Texture2D>(file, stream, false);
                            else if (type == typeof(Takai.Graphics.BitmapFont))
                                dat = Load<Takai.Graphics.BitmapFont>(file, stream, false);
                            else if (type == typeof(SoundEffect))
                                dat = Load<SoundEffect>(file, stream, false);

                            if (dat == null)
                                result = false;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Load an asset into the manager, specify load-over to load the new asset over an older one at a name clash
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="File">The name of the file to load. Will be used as the asset's name by default</param>
        /// <param name="OverWrite">If there is a name clash, unload the old one and load this over if true</param>
        /// <param name="CustomName">Use this name as opposed to the file to idenfity this asset</param>
        /// <returns>The asset loaded or if the asset (same name) is already in the manager, it is returned. Null on unrecognized type/error</returns>
        public static T Load<T>(string File, bool OverWrite = false, string CustomName = null)
        {
            string file = (Path.IsPathRooted(File) || File.StartsWith(RootDirectory)) ? File : System.IO.Path.Combine(RootDirectory, File);
            if (CustomName == null)
                CustomName = file;

            IDisposable existing;
            if (assets.TryGetValue(CustomName, out existing))
            {
                if (OverWrite)
                    Unload(CustomName);
                else
                    return (T)existing;
            }
            
            if (System.IO.File.Exists(file))
            {
                var f = new FileStream(file, FileMode.Open);
                var dat = LoadData<T>(CustomName, f);
                f.Close();
                return dat;
            }

            return default(T);
        }

        /// <summary>
        /// Load an asset into the manager, overwriting the old one if found (it is unloaded first)
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="Name">The unique name of the asset</param>
        /// <param name="Stream">The stream to load from</param>
        /// <param name="OverWrite">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset loaded or if the asset (same name) is already in the manager, it is returned. Null on unrecognized type/error</returns>
        public static T Load<T>(string Name, Stream Stream, bool OverWrite)
        {
            if (assets.ContainsKey(Name))
            {
                if (OverWrite)
                {
                    Unload(Name);
                    LoadData<T>(Name, Stream);
                }
                return (T)assets[Name];
            }

            return LoadData<T>(Name, Stream);
        }

        /// <summary>
        /// Load data directly from a source
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <param name="Name">The name of the asset to be stored under</param>
        /// <param name="Stream">The stream of data to load from</param>
        /// <returns>The loaded asset, default(T) on failure</returns>
        private static T LoadData<T>(string Name, Stream Stream)
        {
            IDisposable asset = null;

            if (typeof(T) == typeof(Texture2D))
                asset = Texture2D.FromStream(GraphicsDevice, Stream);
            else if (typeof(T) == typeof(Takai.Graphics.BitmapFont))
                asset = Takai.Graphics.BitmapFont.FromStream(GraphicsDevice, Stream);
            else if (typeof(T) == typeof(SoundEffect))
                asset = SoundEffect.FromStream(Stream);
            else if (typeof(T) == typeof(Effect))
                asset = new Effect(GraphicsDevice, ReadToEnd(Stream));
            
            if (asset != null)
            {
                assets.Add(Name, asset);
                
                var gfx = asset as GraphicsResource;
                if (gfx != null)
                    gfx.Name = Name;

                return (T)asset;
            }

            return default(T);
        }

        private static byte[] ReadToEnd(Stream Stream)
        {
            using (var ms = new MemoryStream())
            {
                var bytes = new byte[16 * 1024];
                int count = 0;

                while ((count = Stream.Read(bytes, 0, bytes.Length)) != 0)
                    ms.Write(bytes, 0, count);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Load an asset from a resource
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="Name">The name of the asset to load</param>
        /// <param name="Resource">The actual resource to load</param>
        /// <param name="OverWrite">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset if loaded, null if not</returns>
        public static T LoadFromResource<T>(string Name, object Resource, bool OverWrite)
        {
            if (!OverWrite && assets.ContainsKey(Name))
            {
                return (T)assets[Name];
            }

            try
            {
                if (typeof(T) == typeof(Texture2D))
                {
#if WINDOWS
                    var bmp = Resource as System.Drawing.Bitmap;
                    MemoryStream ms = new MemoryStream(bmp.Width * bmp.Height);

                    bmp.Save(ms, bmp.RawFormat);
                    IDisposable tex = Load<Texture2D>(Name, ms, OverWrite);
                    ms.Close();
                    return (T)tex;
#else
                    return default(T);
#endif
                }
                else if (typeof(T) == typeof(Takai.Graphics.BitmapFont))
                    return (T)(IDisposable)Load<Takai.Graphics.BitmapFont>(Name, new MemoryStream((byte[])Resource), OverWrite);
            }
            catch { }

            return default(T);
        }

        /// <summary>
        /// Add or replace an existing asset to the asset manager (it can be any type of IDisposable)
        /// </summary>
        /// <typeparam name="T">The type of asset to add</typeparam>
        /// <param name="Name">The name of the asset</param>
        /// <param name="Object">The object itself to store</param>
        /// <remarks>This will overwrite any existing asset</remarks>
        public static void Add<T>(string Name, T Object)
        {
            if (Object.GetType() != typeof(IDisposable))
                return;

            if (assets.ContainsKey(Name))
                assets.Remove(Name);
            assets.Add(Name, (IDisposable)Object);
        }

        /// <summary>
        /// Retrieve an asset if it exists
        /// </summary>
        /// <typeparam name="T">The type of asset</typeparam>
        /// <param name="name">The name of the asset</param>
        /// <returns>The asset if found, null if not</returns>
        /// <remarks>If BeginClean() has been called and the asset is found, it will be checked and will not be removed at EndClean()</remarks>
        public static T Find<T>(string Name)
        {
            IDisposable ass;
            if (assets.TryGetValue(Name, out ass))
            {
                return (T)ass;
            }

            return default(T);
        }

        /// <summary>
        /// Find the name of an asset (using a linear search)
        /// </summary>
        /// <param name="Asset">The asset to be named</param>
        /// <returns>The name if found, an empty string if not</returns>
        public static string FindName(IDisposable Asset)
        {
            foreach (var asset in assets)
                if (asset.Value == Asset)
                    return asset.Key;
            return string.Empty;
        }

        /// <summary>
        /// Unload a specific asset, if not found, nothing happens
        /// </summary>
        /// <param name="name">The asset to remove</param>
        public static void Unload(string Name)
        {
            IDisposable ass;
            if (assets.TryGetValue(Name, out ass))
            {
                assets.Remove(Name);
                ass.Dispose();
                GC.SuppressFinalize(ass);
            }
        }

        /// <summary>
        /// Destroy all assets in the manager
        /// </summary>
        public static void UnloadAll()
        {
            foreach (var n in assets)
            {
                n.Value.Dispose();
                GC.SuppressFinalize(n.Value);
            }

            assets.Clear();
        }

        /// <summary>
        /// The enumerator for the assets
        /// </summary>
        public static Dictionary<string, IDisposable>.Enumerator GetEnumerator()
        {
            return assets.GetEnumerator();
        }

        //Export the names of all of the loaded assets
        public static void ExportNames(Stream Stream, String Separator)
        {
            if (assets.Count < 1)
                return;
            
            bool first = true;
            foreach (var key in assets.Keys)
            {
                var bytes = System.Text.Encoding.Unicode.GetBytes((first ? "" : Separator) + key);
                Stream.Write(bytes, 0, bytes.Length);
                first = false;
            }
        }
    }
}
