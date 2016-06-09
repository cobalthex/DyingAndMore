using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO.Compression;

namespace Takai
{

    /// <summary>
    /// A simple name based asset managment system that will hold various asset types and manage the lifetime of them
    /// </summary>
    public class AssetManager
    {
        /// <summary>
        /// The root directory to search for files for this asset manager
        /// </summary>
        public string rootDirectory;

        /// <summary>
        /// The assets themselves
        /// </summary>
        private System.Collections.Generic.Dictionary<string, IDisposable> assets;

        /// <summary>
        /// The graphics device used for this asset manager (for creating texture assets)
        /// </summary>
        public Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice;

        /// <summary>
        /// The total number of loaded assets in the manager
        /// </summary>
        public int totalAssets { get { return assets.Count; } }

        /// <summary>
        /// Create a new asset manager
        /// </summary>
        /// <param name="GDev">The graphics device to use</param>
        public AssetManager(Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice)
        {
            graphicsDevice = GraphicsDevice;
            assets = new System.Collections.Generic.Dictionary<string, IDisposable>(64);
            rootDirectory = "";
        }

        /// <summary>
        /// Create a new asset manager
        /// </summary>
        /// <param name="GDev">The graphics device to use</param>
        /// <param name="RootDirector">The optional root directory for asset searching</param>
        public AssetManager(Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice, string RootDirectory)
        {
            graphicsDevice = GraphicsDevice;
            assets = new System.Collections.Generic.Dictionary<string, IDisposable>(64);
            rootDirectory = RootDirectory;
        }

        /// <summary>
        /// Load multiple assets from a package file
        /// </summary>
        /// <param name="File">The file to load from</param>
        /// <param name="IncludeExtensions">Include file extensions in the asset's name</param>
        /// <param name="CheckDirectory">Check the directory for a file of a matching name and use it instead</param>
        /// <remarks>If the file is not found, a FileNotFoundException is thrown</remarks>
        public void LoadZip(string File, bool IncludeExtensions, bool CheckDirectory)
        {
            string file = System.IO.Path.IsPathRooted(File) ? File : System.IO.Path.Combine(rootDirectory, File);
#if WINDOWS
            if (System.IO.File.Exists(file))
            {
                FileStream f = new FileStream(file, FileMode.Open);
                LoadZip(f, IncludeExtensions, System.IO.Path.GetDirectoryName(file));
                f.Close();
            }
            else
                throw new FileNotFoundException(rootDirectory + "/" + File + " could not be found");
#else
            var stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(file);
            if (stream != null)
            {
                LoadZip(stream, IncludeExtensions, System.IO.Path.GetDirectoryName(file));
                stream.Close();
            }
#endif
        }

        /// <summary>
        /// Get the type of file from its extension
        /// </summary>
        /// <param name="Extension">The file extension (with/without the period in front)</param>
        /// <returns>The type of asset or null if none found</returns>
        protected System.Type GetAssetType(string Extension)
        {
            if (Extension[0] == '.')
                Extension = Extension.Substring(1);
            if (Extension == "png" || Extension == "jpg" || Extension == "jpeg" || Extension == "tga" || Extension == "tif" || Extension == "tiff" || Extension == "bmp")
                return typeof(Texture2D);
            if (Extension == "bfnt")
                return typeof(Takai.Graphics.BitmapFont);
            if (Extension == "mp3" || Extension == "wav" || Extension == "wma" || Extension == "ogg")
                return typeof(SoundEffect);
            return null;
        }

        /// <summary>
        /// Load multiple assets from a package stream, does not load new assets over old ones
        /// </summary>
        /// <param name="Stream">The stream to load from</param>
        /// <param name="IncludeExtensions">Include file extensions in the asset's name</param>
        /// <returns>True on successful load of all assets. False on error loading one or more assets (All assets will be attempted to be loaded)</returns>
        public bool LoadZip(Stream Stream, bool IncludeExtensions)
        {
            return LoadZip(Stream, IncludeExtensions, null);
        }

        //todo: verify (changed to using native zip archive library)
        bool LoadZip(Stream Stream, bool IncludeExtensions, string CheckDir)
        {
            bool result = true;
            using (var zip = new ZipArchive(Stream, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    var type = GetAssetType(Path.GetExtension(entry.FullName));
                    var file = IncludeExtensions ? entry.FullName : Path.GetFileNameWithoutExtension(entry.Name);

                    if (CheckDir != null)
                    {
                        var path = System.IO.Path.Combine(CheckDir, entry.FullName);
                        if (System.IO.File.Exists(path))
                        {
                            path = path.Substring(rootDirectory.Length);
                            IDisposable dat = null;
                            if (type == typeof(Texture2D))
                                dat = Load<Texture2D>(file, path, false);
                            else if (type == typeof(Takai.Graphics.BitmapFont))
                                dat = Load<Takai.Graphics.BitmapFont>(file, path, false);
                            else if (type == typeof(SoundEffect))
                                dat = Load<SoundEffect>(file, path, false);

                            if (dat == null)
                                result = false;

                            continue;
                        }
                    }
                        
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
        /// Load an asset, replacing the old if an old one of the same name exists
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="Name">The name of the asset to be stored under</param>
        /// <param name="File">The file to load the asset from</param>
        /// <returns>The loaded asset, default(T) if not able to load</returns>
        public T Load<T>(string Name, string File)
        {
            return Load<T>(Name, File, false);
        }
        
        /// <summary>
        /// Load an asset into the manager, specify load-over to load the new asset over an older one at a name clash
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="Name">The unique name of the asset</param>
        /// <param name="File">The file to load from</param>
        /// <param name="LoadOver">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset loaded or if the asset (same name) is already in the manager, it is returned. Null on unrecognized type/error</returns>
        public T Load<T>(string Name, string File, bool LoadOver)
        {
            string file = System.IO.Path.IsPathRooted(File) ? File : System.IO.Path.Combine(rootDirectory, File);

            if (!System.IO.File.Exists(file))
                return (T)assets[Name];
            if (assets.ContainsKey(Name))
            {
                if (LoadOver)
                    Unload(Name);
                else
                    return (T)assets[Name];
            }

#if WINDOWS
            if (System.IO.File.Exists(file))
            {
                var f = new FileStream(file, FileMode.Open);
                var dat = LoadData<T>(Name, f);
                f.Close();
                return dat;
            }
#else
            var stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(file);
            if (stream != null)
            {
                var dat = LoadData<T>(Name, stream);
                stream.Close();
                return dat;
            }
#endif

            return default(T);
        }

        /// <summary>
        /// Load an asset into the manager, overwriting the old one if found (it is unloaded first)
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="Name">The unique name of the asset</param>
        /// <param name="Stream">The stream to load from</param>
        /// <param name="LoadOver">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset loaded or if the asset (same name) is already in the manager, it is returned. Null on unrecognized type/error</returns>
        public T Load<T>(string Name, Stream Stream, bool LoadOver)
        {
            if (assets.ContainsKey(Name))
            {
                if (LoadOver)
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
        private T LoadData<T>(string Name, Stream Stream)
        {
            IDisposable asset = null;

            if (typeof(T) == typeof(Texture2D))
                asset = Texture2D.FromStream(graphicsDevice, Stream);
            else if (typeof(T) == typeof(Takai.Graphics.BitmapFont))
                asset = Takai.Graphics.BitmapFont.FromStream(graphicsDevice, Stream);
            else if (typeof(T) == typeof(SoundEffect))
                asset = SoundEffect.FromStream(Stream);

            if (asset != null)
            {
                assets.Add(Name, asset);
                return (T)asset;
            }

            return default(T);
        }

        /// <summary>
        /// Load an asset from a resource
        /// </summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="Name">The name of the asset to load</param>
        /// <param name="Resource">The actual resource to load</param>
        /// <param name="LoadOver">If there is a name clash, unload the old one and load this over if true</param>
        /// <returns>The asset if loaded, null if not</returns>
        public T LoadFromResource<T>(string Name, object Resource, bool LoadOver)
        {
            if (assets.ContainsKey(Name))
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
                    IDisposable tex = Load<Texture2D>(Name, ms, LoadOver);
                    ms.Close();
                    return (T)tex;
#else
                    return default(T);
#endif
                }
                else if (typeof(T) == typeof(Takai.Graphics.BitmapFont))
                    return (T)(IDisposable)Load<Takai.Graphics.BitmapFont>(Name, new MemoryStream((byte[])Resource), LoadOver);
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
        public void Add<T>(string Name, T Object)
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
        public T Find<T>(string Name)
        {
            IDisposable ass;
            if (assets.TryGetValue(Name, out ass))
            {
                return (T)ass;
            }

            return default(T);
        }

        /// <summary>
        /// Find the name of an asset based on itself (using a linear search)
        /// </summary>
        /// <param name="Asset">The asset to be named</param>
        /// <returns>The name if found, an empty string if not</returns>
        public string FindName(IDisposable Asset)
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
        public void Unload(string Name)
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
        public void UnloadAll()
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
        public System.Collections.Generic.Dictionary<string, IDisposable>.Enumerator GetEnumerator()
        {
            return assets.GetEnumerator();
        }
    }
}
