using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Data
{
    /// <summary>
    /// A cache to store previously loaded files
    /// </summary>
    public static class Cache
    {
        public static string DefaultRoot = "Content";

        public struct CustomLoad
        {
            public string name;
            public string file;
            public Stream stream;
            public long length;
        }

        struct CachedFile
        {
            //file name?
            public WeakReference reference;
            public DateTime fileTime; //last modified time when file was loaded (in UTC)
        }

        /// <summary>
        /// Custom loaders for specific file extensions (Do not include the first . in the extension)
        /// All other formats will be deserialized using the Serializer
        /// </summary>
        public static Dictionary<string, Func<CustomLoad, object>> CustomLoaders { get; private set; }
            = new Dictionary<string, Func<CustomLoad, object>>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, CachedFile> objects;
        public static IEnumerable<KeyValuePair<string, object>> Objects
        {
            get
            {
                foreach (var obj in objects)
                {
                    if (obj.Value.reference.IsAlive)
                        yield return new KeyValuePair<string, object>(obj.Key, obj.Value.reference.Target);
                }
            }
        }

        private static Dictionary<string, ZipArchive> openZips = new Dictionary<string, ZipArchive>(); //todo: load files into case-insensitive dictionary

        static Cache()
        {
            objects = new Dictionary<string, CachedFile>();

            CustomLoaders.Add("png", LoadTexture);
            CustomLoaders.Add("jpg", LoadTexture);
            CustomLoaders.Add("jpeg", LoadTexture);
            CustomLoaders.Add("tga", LoadTexture);
            CustomLoaders.Add("tiff", LoadTexture);
            CustomLoaders.Add("bmp", LoadTexture);
            CustomLoaders.Add("gif", LoadTexture);

            CustomLoaders.Add("bfnt", LoadBitmapFont);

            CustomLoaders.Add("ogg", LoadOgg);
            CustomLoaders.Add("wav", LoadSound);
            CustomLoaders.Add("mp3", UnsuportedExtension);

            CustomLoaders.Add("mgfx", LoadEffect);
        }

        /// <summary>
        /// Retrieve an item from the cache
        /// </summary>
        /// <param name="file">The file to find</param>
        /// <returns>The cached object, or null if not found/deleted</returns>
        public static object Get(string file)
        {
            if (objects.TryGetValue(file, out var cache))
                return cache.reference.Target;
            return null;
        }

        /// <summary>
        /// Load all entries of a zip file into the cache
        /// </summary>
        /// <param name="file">The (absolute or working-directory relative) location of the zip</param>
        public static IEnumerable<object> LoadZip(string file, bool forceLoad = false)
        {
            using (var zip = new ZipArchive(File.OpenRead(file), ZipArchiveMode.Read, false))
            {
                openZips.Add(file, zip);
                foreach (var entry in zip.Entries)
                    yield return Load(entry.FullName, file, forceLoad);
                openZips.Remove(file);
            }
        }

        /// <summary>
        /// Load a single file into the cache
        /// </summary>
        /// <param name="file">The relative location of file to load from</param>
        /// <param name="root">Where to search for the file. This is passed to any recursive loads. Use "" to load from working directory, null to load from DefaultRoot</param>
        /// <param name="forceLoad">Load the file even if it already exists in the cache</param>
        /// <returns>The loaded object</returns>
        public static object Load(string file, string root = null, bool forceLoad = false)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException(nameof(file) + " cannot be null or empty");

            root = root ?? DefaultRoot;
            file = Normalize(file);

            string realFile;
            if (PathStartsWith(file, DefaultRoot) && root == DefaultRoot)
            {
                realFile = file;
                file = file.Substring(DefaultRoot.Length + 1);
            }
            else if (Path.IsPathRooted(file) || PathStartsWith(file, root))
                realFile = file;
            else
                realFile = Normalize(Path.Combine(root, file));

            bool exists = objects.TryGetValue(realFile, out var obj) && obj.reference.IsAlive;
            if (forceLoad || !exists)
            {
                var load = new CustomLoad { name = file, file = realFile };
                if (root != null && openZips.TryGetValue(root, out var zip))
                {
                    var entry = zip.GetEntry(file);
                    if (entry == null)
                        return Load(file, null, forceLoad); //try loading from default location

                    load.length = entry.Length;

                    //zip streams do not support seeking
                    var memStream = new MemoryStream();
                    using (var stream = entry.Open())
                        stream.CopyTo(memStream);

                    memStream.Seek(0, SeekOrigin.Begin);
                    load.stream = memStream;

                    obj.fileTime = entry.LastWriteTime.UtcDateTime;
                }
                else
                {
                    var fi = new FileInfo(realFile);
                    obj.fileTime = fi.LastWriteTimeUtc;
                    load.length = fi.Length;
                    load.stream = fi.OpenRead();
                }

                var ext = GetExtension(file);
                if (CustomLoaders.TryGetValue(ext, out var loader))
                    obj.reference = new WeakReference(loader?.Invoke(load));
                else
                {
                    var context = new Serializer.DeserializationContext
                    {
                        reader = new StreamReader(load.stream),
                        file = file,
                        root = root,
                    };
                    obj.reference = new WeakReference(Serializer.TextDeserialize(context));
                    if (obj.reference.Target is ISerializeExternally sxt)
                        sxt.File = file;
                }
                load.stream.Dispose();
            }

            if (forceLoad && exists)
            {
                //todo: specific option for applying rather than replacing (and maybe serializer.get_dictionary)
                Serializer.ApplyObject(objects[realFile].reference.Target, Serializer.Cast(objects[realFile].reference.Target.GetType(), obj.reference.Target));
                obj = objects[realFile];
            }
            else
                objects[realFile] = obj;

            return obj.reference.Target;
        }


        /// <summary>
        /// Load a single file into the cache
        /// </summary>
        /// <typeparam name="T">The type to cast this object to</typeparam>
        /// <param name="file">The relative location of file to load from</param>
        /// <param name="root">Where to search for the file. This is passed to any recursive loads</param>
        /// <param name="forceLoad">Load the file even if it already exists in the cache</param>
        /// <returns>The loaded object, casted to <typeparamref name="T"/></returns>
        public static T Load<T>(string file, string root = null, bool forceLoad = false)
        {
            return Serializer.Cast<T>(Load(file, root, forceLoad));
        }

        public static void SaveAllToFile(string file)
        {
            using (var writer = new StreamWriter(file))
                Serializer.TextSerialize(writer, objects, 0, true);
        }

        public static string Normalize(string path)
        {
            return path.Replace("\\\\", "/").Replace('\\', '/');
        }

        public static bool PathStartsWith(string path, string root)
        {
            return (path.StartsWith(root) && (path.Length <= root.Length || path[root.Length] == '\\' || path[root.Length] == '/'));
        }

        public static string GetExtension(string path)
        {
            return path.Substring(path.LastIndexOf('.') + 1);

            //var dir = path.LastIndexOfAny(new[] { '\\', '/' }) + 1;
            //var ext = path.IndexOf('.', dir) + 1;
            //return path.Substring(ext == 0 ? dir : ext);
        }

        /// <summary>
        /// Remove any objects from the cache that are not used elsewhere
        /// </summary>
        /// <param name="gcCollect">Call <see cref="GC.Collect"/> first (Will make sure all objects are found)</param>
        public static void CleanupStaleReferences(bool gcCollect = true)
        {
            if (gcCollect)
            {
                GC.WaitForPendingFinalizers(); //necessary?
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            }

            var stale = new List<string>();

            foreach (var obj in objects)
            {
                if (!obj.Value.reference.IsAlive)
                    stale.Add(obj.Key);
            }

            foreach (var key in stale)
                objects.Remove(key);
        }

        #region Custom Loaders

        internal static object UnsuportedExtension(CustomLoad load)
        {
            throw new NotSupportedException($"Reading from {Path.GetExtension(load.file)} is not supported");
        }

        internal static object LoadTexture(CustomLoad load)
        {
            var loaded = Texture2D.FromStream(Runtime.GraphicsDevice, load.stream);
            loaded.Name = load.name;
            return loaded;
        }

        internal static object LoadBitmapFont(CustomLoad load)
        {
            var loaded = Graphics.BitmapFont.FromStream(Runtime.GraphicsDevice, load.stream);
            //loaded.Name = load.name; //todo
            return loaded;
        }

        internal static object LoadOgg(CustomLoad load)
        {
            using (var vorbis = new NVorbis.VorbisReader(load.stream, false))
            {
                if (vorbis.Channels < 1 || vorbis.Channels > 2)
                    throw new FormatException($"Audio must be in mono or stero (provided: {vorbis.Channels})");

                if (vorbis.SampleRate < 8000 || vorbis.SampleRate > 48000)
                    throw new FormatException($"Audio must be between 8kHz and 48kHz (provided: {vorbis.SampleRate}Hz");

                //todo: 16bit pcm can maybe be retrieved directly from load

                var total = (int)(vorbis.TotalSamples * vorbis.Channels);

                var buffer = new float[total]; //-1 to 1
                if (vorbis.ReadSamples(buffer, 0, total) <= 0)
                    throw new IOException("Error reading Ogg Vorbis samples");

                //convert 32 bit float to 16 bit PCM
                //todo: aliasing issues?
                var samples = new byte[total * 2];
                for (int i = 0; i < buffer.Length; ++i)
                {
                    var n = (short)(buffer[i] * 0x8000);
                    samples[i * 2] = (byte)(n & 0xff);
                    samples[i * 2 + 1] = (byte)((n >> 8) & 0xff);
                }

                return new SoundEffect(samples, vorbis.SampleRate, (AudioChannels)vorbis.Channels)
                {
                    Name = load.name
                };
            }
        }

        internal static object LoadSound(CustomLoad load)
        {
            var loaded = SoundEffect.FromStream(load.stream);
            loaded.Name = load.name;
            return loaded;
        }

        internal static object LoadEffect(CustomLoad load)
        {
            var bytes = new byte[load.length];
            load.stream.Read(bytes, 0, bytes.Length);

            //load = TransformPath(file, DataFolder, "Shaders", "DX11");
            return new Effect(Runtime.GraphicsDevice, bytes)
            {
                Name = load.name
            };
        }

        internal static Dictionary<string, FileSystemWatcher> fsWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
        public static void WatchDirectory(string directory, string filter = "*.*")
        {
            var watcher = new FileSystemWatcher(directory, filter)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            watcher.Changed += Watcher_Changed;
            fsWatchers[directory] = watcher;
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //this may be called multiple times: https://blogs.msdn.microsoft.com/oldnewthing/20140507-00/?p=1053/
            System.Threading.Thread.Sleep(500);
            try
            {
                var path = Normalize(e.FullPath);
                if (objects.ContainsKey(path))
                {
                    Load(path, null, true);
                    LogBuffer.Append("Refreshed " + e.FullPath);
                }
            }
            catch (Exception ex)
            {
                LogBuffer.Append($"Failed to refresh {e.FullPath} ({ex.Message})");
            }
        }

        #endregion
    }
}