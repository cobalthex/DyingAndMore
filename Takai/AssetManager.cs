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
        /// <param name="graphicsDevice">The graphics device to use for texture ops</param>
        /// <param name="rootDirectory">The root directory to search for assets. By default is ''</param>
        public static void Initialize(string rootDirectory = "")
        {
            RootDirectory = rootDirectory;
            assets = new Dictionary<string, IDisposable>();
        }

        public static Dictionary<string, Type> AssetTypes { get; set; } = new Dictionary<string, System.Type>
        {
            { "png", typeof(Texture2D) },
            { "jpg", typeof(Texture2D) },
            { "jpeg", typeof(Texture2D) },
            { "tga", typeof(Texture2D) },
            { "tif", typeof(Texture2D) },
            { "tiff", typeof(Texture2D) },
            { "bmp", typeof(Texture2D) },

            { "bfnt", typeof(Graphics.BitmapFont) },

            { "wav", typeof(SoundEffect) },
            { "ogg", typeof(SoundEffect) },
            //{ "mp3", typeof(SoundEffect) },
            //{ "wma", typeof(SoundEffect) },

            { "fx", typeof(Effect) },
            { "mgfx", typeof(Effect) },
        };

        static bool LoadZip(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
                return LoadZip(stream);
        }

        /// <summary>
        /// Load multiple assets from a package stream, does not load new assets over old ones
        /// </summary>
        /// <param name="zipStream">The stream to load from</param>
        /// <param name="IncludeExtensions">Include file extensions in the asset's name</param>
        /// <returns>True on successful load of all assets. False on error loading one or more assets (All assets will be attempted to be loaded)</returns>
        static bool LoadZip(Stream zipStream)
        {
            bool result = true;
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
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
        /// <param name="file">The name of the file to load. Will be used as the asset's name by default</param>
        /// <param name="overwrite">If there is a name clash, unload the old one and load this over if true</param>
        /// <param name="customName">Use this name as opposed to the file to idenfity this asset</param>
        /// <returns>The asset loaded or if the asset (same name) is already in the manager, it is returned. Null on unrecognized type/error</returns>
        public static T Load<T>(string file, bool overwrite = false, string customName = null)
        {
            file = (Path.IsPathRooted(file) || file.StartsWith(RootDirectory)) ? file : Path.Combine(RootDirectory, file);
            if (customName == null)
                customName = file;

            if (assets.TryGetValue(customName, out IDisposable existing))
            {
                if (overwrite)
                    Unload(customName);
                else
                    return (T)existing;
            }

            if (File.Exists(file))
            {
                var f = new FileStream(file, FileMode.Open);
                var dat = LoadData<T>(customName, f);
                f.Close();
                return dat;
            }

            return default(T);
        }

        /// <summary>
        /// Load an asset into the manager, overwriting the old one if found (it is unloaded first)
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="name">The unique name of the asset</param>
        /// <param name="stream">The stream to load from</param>
        /// <param name="overwrite">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset loaded or if the asset (same name) is already in the manager, it is returned. Null on unrecognized type/error</returns>
        public static T Load<T>(string name, Stream stream, bool overwrite)
        {
            if (assets.ContainsKey(name))
            {
                if (overwrite)
                {
                    Unload(name);
                    LoadData<T>(name, stream);
                }
                return (T)assets[name];
            }

            return LoadData<T>(name, stream);
        }

        /// <summary>
        /// Load data directly from a source
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <param name="name">The name of the asset to be stored under</param>
        /// <param name="stream">The stream of data to load from</param>
        /// <returns>The loaded asset, default(T) on failure</returns>
        private static T LoadData<T>(string name, Stream stream)
        {
            IDisposable asset = null;

            if (typeof(T) == typeof(Texture2D))
                asset = Texture2D.FromStream(Runtime.GraphicsDevice, stream);
            else if (typeof(T) == typeof(Takai.Graphics.BitmapFont))
                asset = Takai.Graphics.BitmapFont.FromStream(Runtime.GraphicsDevice, stream);
            else if (typeof(T) == typeof(SoundEffect))
            {
                if (name.EndsWith(".ogg"))
                    asset = LoadOgg(stream);
                else
                    asset = SoundEffect.FromStream(stream);
            }
            else if (typeof(T) == typeof(Effect))
                asset = new Effect(Runtime.GraphicsDevice, ReadToEnd(stream));
            
            if (asset != null)
            {
                assets.Add(name, asset);

                if (asset is GraphicsResource gfx)
                    gfx.Name = name;

                return (T)asset;
            }

            return default(T);
        }

        public static SoundEffect LoadOgg(Stream stream)
        {
            using (var vorbis = new NVorbis.VorbisReader(stream, false))
            {
                if (vorbis.Channels < 1 || vorbis.Channels > 2)
                    throw new FormatException($"Audio must be in mono or stero (provided: {vorbis.Channels})");

                if (vorbis.SampleRate < 8000 || vorbis.SampleRate > 48000)
                    throw new FormatException($"Audio must be between 8kHz and 48kHz (provided: {vorbis.SampleRate}Hz");

                //todo: 16bit pcm can maybe be retrieved direcltly from file

                var total = (int)(vorbis.TotalSamples * vorbis.Channels);

                var buffer = new float[total]; //-1 to 1
                if (vorbis.ReadSamples(buffer, 0, total) <= 0)
                    throw new IOException("Error reading Ogg Vorbis samples"); //todo: better exception?

                //convert 32 bit float to 16 bit PCM
                //todo: aliasing issues
                var samples = new byte[total * 2];
                for (int i = 0; i < buffer.Length; ++i)
                {
                    var n = (short)(buffer[i] * 0x8000);
                    samples[i * 2] = (byte)(n & 0xff);
                    samples[i * 2 + 1] = (byte)((n >> 8) & 0xff);
                }
                
                return new SoundEffect(samples, vorbis.SampleRate, (AudioChannels)vorbis.Channels);
            }
        }

        private static byte[] ReadToEnd(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                var bytes = new byte[16 * 1024];
                int count = 0;

                while ((count = stream.Read(bytes, 0, bytes.Length)) != 0)
                    ms.Write(bytes, 0, count);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Load an asset from a resource
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="name">The name of the asset to load</param>
        /// <param name="resource">The actual resource to load</param>
        /// <param name="overwrite">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset if loaded, null if not</returns>
        public static T LoadFromResource<T>(string name, object resource, bool overwrite)
        {
            if (!overwrite && assets.ContainsKey(name))
            {
                return (T)assets[name];
            }

            try
            {
                if (typeof(T) == typeof(Texture2D))
                {
#if WINDOWS
                    var bmp = resource as System.Drawing.Bitmap;
                    MemoryStream ms = new MemoryStream(bmp.Width * bmp.Height);

                    bmp.Save(ms, bmp.RawFormat);
                    IDisposable tex = Load<Texture2D>(name, ms, overwrite);
                    ms.Close();
                    return (T)tex;
#else
                    return default(T);
#endif
                }
                else if (typeof(T) == typeof(Takai.Graphics.BitmapFont))
                    return (T)(IDisposable)Load<Takai.Graphics.BitmapFont>(name, new MemoryStream((byte[])resource), overwrite);
            }
            catch { }

            return default(T);
        }

        /// <summary>
        /// Add or replace an existing asset to the asset manager (it can be any type of IDisposable)
        /// </summary>
        /// <typeparam name="T">The type of asset to add</typeparam>
        /// <param name="name">The name of the asset</param>
        /// <param name="asset">The object itself to store</param>
        /// <remarks>This will overwrite any existing asset</remarks>
        public static void Add<T>(string name, T asset)
        {
            if (asset.GetType() != typeof(IDisposable))
                return;

            if (assets.ContainsKey(name))
                assets.Remove(name);
            assets.Add(name, (IDisposable)asset);
        }

        /// <summary>
        /// Retrieve an asset if it exists
        /// </summary>
        /// <typeparam name="T">The type of asset</typeparam>
        /// <param name="name">The name of the asset</param>
        /// <returns>The asset if found, null if not</returns>
        /// <remarks>If BeginClean() has been called and the asset is found, it will be checked and will not be removed at EndClean()</remarks>
        public static T Find<T>(string name)
        {
            if (assets.TryGetValue(name, out IDisposable ass))
            {
                return (T)ass;
            }

            return default(T);
        }

        /// <summary>
        /// Find the name of an asset (using a linear search)
        /// </summary>
        /// <param name="asset">The asset to be named</param>
        /// <returns>The name if found, an empty string if not</returns>
        public static string FindName(IDisposable asset)
        {
            foreach (var ass in assets)
                if (ass.Value == asset)
                    return ass.Key;
            return string.Empty;
        }

        /// <summary>
        /// Unload a specific asset, if not found, nothing happens
        /// </summary>
        /// <param name="name">The asset to remove</param>
        public static void Unload(string name)
        {
            if (assets.TryGetValue(name, out IDisposable ass))
            {
                assets.Remove(name);
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
    }
}
