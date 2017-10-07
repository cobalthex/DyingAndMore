using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Takai.Data
{
    /// <summary>
    /// A store for all deserialized files
    /// </summary>
    public static class Cache
    {
        public struct CacheRef
        {
            public object value;
            internal uint generation; //set to uint.MaxValue to make permanent

            public CacheRef(object value)
            {
                this.value = value;
                generation = 0;
            }
        }

        public static string DefaultRoot = "";

        public struct CustomLoad
        {
            public string file;
            public Stream stream;
            public long length;
        }

        /// <summary>
        /// Custom loaders for specific file extensions (Do not include the first . in the extension)
        /// All other formats will be deserialized using the Serializer
        /// </summary>
        public static Dictionary<string, System.Func<CustomLoad, object>> CustomLoaders { get; private set; }
            = new Dictionary<string, System.Func<CustomLoad, object>>();

        public static ReadOnlyDictionary<string, CacheRef> Objects { get; private set; }
        private static Dictionary<string, CacheRef> objects;
        private static uint generation = 0;

        private static Dictionary<string, ZipArchive> openZips = new Dictionary<string, ZipArchive>();

        public static bool DeserializeStrict = true;

        static Cache()
        {
            objects = new Dictionary<string, CacheRef>();
            Objects = new ReadOnlyDictionary<string, CacheRef>(objects);

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
        /// Load all entries of a zip file into the cache
        /// </summary>
        /// <param name="file">The location of the zip</param>
        public static void LoadZip(string file, bool forceLoad = false)
        {
            using (var zip = new ZipArchive(File.OpenRead(file), ZipArchiveMode.Read, false))
            {
                openZips.Add(file, zip);
                foreach (var entry in zip.Entries)
                    Load(entry.FullName, file, forceLoad);
                openZips.Remove(file);
            }
        }

        /// <summary>
        /// Load a single file into the cache
        /// </summary>
        /// <param name="file">The relative location of file to load from</param>
        /// <param name="root">Where to search for the file. This is passed to any recursive loads</param>
        /// <param name="forceLoad">Load the file even if it already exists in the cache</param>
        /// <returns>The loaded object</returns>
        public static object Load(string file, string root = null, bool forceLoad = false)
        {
            //if file begins with ./, parse at current folder (should be in deserializer?)

            root = root ?? DefaultRoot;
            var name = Normalize(Path.Combine(root, file));

            bool exists = objects.TryGetValue(name, out var obj);
            if (!exists)
                obj = new CacheRef();

            if (forceLoad || !exists)
            {
                var load = new CustomLoad { file = name };
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
                }
                else
                {
                    var fi = new FileInfo(name);
                    load.length = fi.Length;
                    load.stream = fi.OpenRead();
                }

                var ext = GetExtension(file);
                if (CustomLoaders.TryGetValue(ext, out var loader))
                    obj.value = loader?.Invoke(load);
                else
                {
                    var context = new Serializer.DeserializationContext
                    {
                        stream = new StreamReader(load.stream),
                        file = file,
                        root = root,
                        strict = DeserializeStrict,
                    };
                    obj.value = Serializer.TextDeserialize(context);
                    if (obj.value is ISerializeExternally sxt)
                        sxt.File = name;
                }
                load.stream.Dispose();
            }

            obj.generation = generation;

            objects[file] = obj;

            return obj.value;
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
                Serializer.TextSerialize(writer, Objects, 0, true);
        }

        public static string Normalize(string path)
        {
            return path.Replace("\\\\", "/").Replace('\\', '/');
        }

        public static string GetExtension(string path)
        {
            return path.Substring(path.LastIndexOf('.') + 1);

            //var dir = path.LastIndexOfAny(new[] { '\\', '/' }) + 1;
            //var ext = path.IndexOf('.', dir) + 1;
            //return path.Substring(ext == 0 ? dir : ext);
        }

        public static void TrackReferences()
        {
            unchecked { ++generation; }
        }

        public static void CleanupStaleReferences()
        {
            var stale = new List<string>();
            foreach (var obj in objects)
            {
                if (obj.Value.generation < generation)
                {
                    if (obj.Value.value is System.IDisposable dis)
                        dis.Dispose();
                    stale.Add(obj.Key);
                }
            }

            foreach (var key in stale)
            {
                objects.Remove(key);
            }
        }

        #region Custom Loaders

        internal static object UnsuportedExtension(CustomLoad load)
        {
            throw new System.NotSupportedException($"Reading from {Path.GetExtension(load.file)} is not supported");
        }

        internal static object LoadTexture(CustomLoad load)
        {
            return Texture2D.FromStream(Runtime.GraphicsDevice, load.stream);
        }

        internal static object LoadBitmapFont(CustomLoad load)
        {
            return Graphics.BitmapFont.FromStream(Runtime.GraphicsDevice, load.stream);
        }

        internal static object LoadOgg(CustomLoad load)
        {
            using (var vorbis = new NVorbis.VorbisReader(load.stream, false))
            {
                if (vorbis.Channels < 1 || vorbis.Channels > 2)
                    throw new System.FormatException($"Audio must be in mono or stero (provided: {vorbis.Channels})");

                if (vorbis.SampleRate < 8000 || vorbis.SampleRate > 48000)
                    throw new System.FormatException($"Audio must be between 8kHz and 48kHz (provided: {vorbis.SampleRate}Hz");

                //todo: 16bit pcm can maybe be retrieved direcltly from load

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

        internal static object LoadSound(CustomLoad load)
        {
            return SoundEffect.FromStream(load.stream);
        }

        internal static object LoadEffect(CustomLoad load)
        {
            var bytes = new byte[load.length];
            load.stream.Read(bytes, 0, bytes.Length);

            //load = TransformPath(file, DataFolder, "Shaders", "DX11");
            return new Effect(Runtime.GraphicsDevice, bytes);
        }

        #endregion
    }
}
