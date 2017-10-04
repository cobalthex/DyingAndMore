using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

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

        /// <summary>
        /// Custom loaders for specific file extensions (Do not include the first . in the extension)
        /// All other formats will be deserialized using the Serializer
        /// </summary>
        public static Dictionary<string, System.Func<string, object>> CustomLoaders { get; private set; }
            = new Dictionary<string, System.Func<string, object>>();

        public static ReadOnlyDictionary<string, CacheRef> Objects { get; private set; }
        private static Dictionary<string, CacheRef> objects;
        private static uint generation = 0;

        static Cache()
        {
            objects = new Dictionary<string, CacheRef>();
            Objects = new ReadOnlyDictionary<string, CacheRef>(objects);

            CustomLoaders.Add("png",  LoadTexture);
            CustomLoaders.Add("jpg",  LoadTexture);
            CustomLoaders.Add("jpeg", LoadTexture);
            CustomLoaders.Add("tga",  LoadTexture);
            CustomLoaders.Add("tiff", LoadTexture);
            CustomLoaders.Add("bmp",  LoadTexture);
            CustomLoaders.Add("gif",  LoadTexture);

            CustomLoaders.Add("bfnt", LoadBitmapFont);

            CustomLoaders.Add("ogg", LoadOgg);
            CustomLoaders.Add("wav", LoadSound);
            CustomLoaders.Add("mp3", UnsuportedExtension);

            CustomLoaders.Add("mgfx", LoadEffect);

            //todo: custom loaders load from data/whatever
            //all others load from defs
        }

        //todo: LoadZip

        public static object Load(string file, bool forceLoad = false)
        {
            file = Normalize(file);

            bool exists = objects.TryGetValue(file, out var obj);
            if (!exists)
                obj = new CacheRef();

            if (forceLoad || !exists)
            {
                var ext = Path.GetExtension(file);

                if (CustomLoaders.TryGetValue(ext, out var loader))
                    obj.value = loader?.Invoke(file);
                else
                {
                    obj.value = Serializer.TextDeserialize(file);
                    if (obj.value is ISerializeExternally sxt)
                        sxt.File = file;
                }
            }

            obj.generation = generation;

            objects[file] = obj;

            return obj.value;
        }

        public static T Load<T>(string file, bool forceLoad = false)
        {
            return Serializer.Cast<T>(Load(file, forceLoad));
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

        internal static object UnsuportedExtension(string file)
        {
            throw new System.NotSupportedException($"Reading from {Path.GetExtension(file)} is not supported");
        }

        internal static object LoadTexture(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
                return Texture2D.FromStream(Runtime.GraphicsDevice, stream);
        }

        internal static object LoadBitmapFont(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
                return Graphics.BitmapFont.FromStream(Runtime.GraphicsDevice, stream);
        }

        internal static object LoadOgg(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
            {
                using (var vorbis = new NVorbis.VorbisReader(stream, false))
                {
                    if (vorbis.Channels < 1 || vorbis.Channels > 2)
                        throw new System.FormatException($"Audio must be in mono or stero (provided: {vorbis.Channels})");

                    if (vorbis.SampleRate < 8000 || vorbis.SampleRate > 48000)
                        throw new System.FormatException($"Audio must be between 8kHz and 48kHz (provided: {vorbis.SampleRate}Hz");

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
        }

        internal static object LoadSound(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
                return SoundEffect.FromStream(stream);
        }

        internal static object LoadEffect(string file)
        {
            file = Path.Combine("Shaders", "DX11", file); //todo: more standard method
            return new Effect(Runtime.GraphicsDevice, File.ReadAllBytes(file));
        }

        #endregion
    }
}
