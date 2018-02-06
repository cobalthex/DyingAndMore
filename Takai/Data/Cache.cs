﻿using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Data
{
    /// <summary>
    /// a hacky workaround to the awkwardness that is SoundEffect/instance
    /// </summary>
    public interface ISoundSource : IDisposable
    {
        SoundEffectInstance Instantiate();
    }

    //todo: configurable case-sensitivity

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

        public class LateBindLoad
        {
            public Action<object> setter;
        }

        /// <summary>
        /// Store a list of late bindings, per file
        /// </summary>
        private static Dictionary<string, List<LateBindLoad>> lateLoads = new Dictionary<string, List<LateBindLoad>>(StringComparer.OrdinalIgnoreCase);

        public abstract class CustomLoader
        {
            public virtual bool PreserveStream { get; protected set; } = false; //useful for streaming contexts
            public abstract object Load(CustomLoad load);
        }

        /// <summary>
        /// Custom loaders for specific file extensions (Do not include the first . in the extension)
        /// All other formats will be deserialized using the Serializer
        /// </summary>
        public static Dictionary<string, CustomLoader> CustomLoaders { get; private set; }
            = new Dictionary<string, CustomLoader>(StringComparer.OrdinalIgnoreCase);

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

        private static Dictionary<string, ZipArchive> openZips = new Dictionary<string, ZipArchive>(StringComparer.OrdinalIgnoreCase);

        static Cache()
        {
            objects = new Dictionary<string, CachedFile>(StringComparer.OrdinalIgnoreCase);

            var loadTex = new TextureLoader();
            CustomLoaders.Add("png",  loadTex);
            CustomLoaders.Add("jpg",  loadTex);
            CustomLoaders.Add("jpeg", loadTex);
            CustomLoaders.Add("tga",  loadTex);
            CustomLoaders.Add("tiff", loadTex);
            CustomLoaders.Add("bmp",  loadTex);
            CustomLoaders.Add("gif",  loadTex);

            CustomLoaders.Add("bfnt", new BitmapFontLoader());

            CustomLoaders.Add("ogg", new OggLoader());
            CustomLoaders.Add("wav", new SoundLoader());
            CustomLoaders.Add("mp3", new UnsupportedLoader());

            CustomLoaders.Add("mgfx", new EffectLoader());
            CustomLoaders.Add("fx", new UnsupportedLoader());
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

            //var swatch = System.Diagnostics.Stopwatch.StartNew();

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

            //this file is currently trying to be loaded elsewhere so it will have to be late bound
            if (lateLoads.TryGetValue(realFile, out var lateLoad))
            {
                var late = new LateBindLoad();
                if (lateLoad == null)
                {
                    lateLoad = new List<LateBindLoad>();
                    lateLoads[realFile] = lateLoad;
                }
                lateLoad.Add(late);
                return late;
            }
            else
                lateLoads.Add(realFile, null);

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

                //System.Diagnostics.Debug.WriteLine($"Cache: Loading {load.name} (Size: {(load.length / 1024f):N1} KiB)");

                try
                {
                    var ext = GetExtension(file);
                    if (CustomLoaders.TryGetValue(ext, out var loader))
                    {
                        obj.reference = new WeakReference(loader.Load(load));
                        if (!loader.PreserveStream)
                            load.stream.Dispose();
                    }
                    else
                    {
                        var context = new Serializer.DeserializationContext
                        {
                            reader = new StreamReader(load.stream),
                            file = file,
                            root = root,
                        };

                        var deserialized = Serializer.TextDeserialize(context);

                        obj.reference = new WeakReference(deserialized);
                        if (obj.reference.Target is ISerializeExternally sxt)
                            sxt.File = file;
                        load.stream.Dispose();
                    }
                }
                catch
                {
                    load.stream.Dispose();
                }
            }

            //apply late bound values
            if (lateLoads.TryGetValue(realFile, out lateLoad))
            {
                if (lateLoad != null)
                {
                    foreach (var late in lateLoad)
                        late.setter?.Invoke(obj.reference.Target); //todo: figure out why sometimes null (possibly due to 2 loads in file watcher)
                }
                lateLoads.Remove(realFile);
            }

            if (forceLoad && exists)
            {
                //todo: specific option for applying rather than replacing (and maybe serializer.get_dictionary)
                Serializer.ApplyObject(objects[realFile].reference.Target, Serializer.Cast(objects[realFile].reference.Target.GetType(), obj.reference.Target));
                obj = objects[realFile];
            }
            else
                objects[realFile] = obj;

            //swatch.Stop();
            //System.Diagnostics.Debug.WriteLine($"Cache: loaded {file} in {swatch.ElapsedMilliseconds} msec");

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
            var loaded = Load(file, root, forceLoad);
            return Serializer.Cast<T>(loaded);
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

        #region Custom Loaders

        class UnsupportedLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                throw new NotSupportedException($"Reading from {Path.GetExtension(load.file)} is not supported");
            }
        }

        class TextureLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                var loaded = Texture2D.FromStream(Runtime.GraphicsDevice, load.stream);
                loaded.Name = load.name;
                return loaded;
            }
        }

        class BitmapFontLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                var loaded = Graphics.BitmapFont.FromStream(Runtime.GraphicsDevice, load.stream);
                //loaded.Name = load.name; //todo
                return loaded;
            }
        }
        public class SoundEffectSoundSource : ISoundSource
        {
            public SoundEffect sound;

            public SoundEffectInstance Instantiate()
            {
                return sound.CreateInstance();
            }

            public void Dispose()
            {
                sound.Dispose();
            }
        }

        public class OggSoundSource : ISoundSource
        {
            public NVorbis.VorbisReader vorbis;

            //assumes single threaded
            float[] intermediate; //might be able to get directly from ogg
            byte[] samples;
            long readOffset;

            //nvorbis logical streams?

            public OggSoundSource(NVorbis.VorbisReader vorbis)
            {
                this.vorbis = vorbis;
                var sampleCount = Math.Min(vorbis.TotalSamples, 128 * 1024);
                intermediate = new float[sampleCount];
                samples = new byte[sampleCount * 2]; //16 bit PCM
            }

            public SoundEffectInstance Instantiate()
            {
                var instance = new DynamicSoundEffectInstance(vorbis.SampleRate, (AudioChannels)vorbis.Channels);
                instance.BufferNeeded += Instance_BufferNeeded;
                Instance_BufferNeeded(instance, EventArgs.Empty); //preload first buffer
                return instance;
            }

            private void Instance_BufferNeeded(object sender, EventArgs e)
            {
                var instance = (DynamicSoundEffectInstance)sender;

                var read = vorbis.ReadSamples(intermediate, 0, intermediate.Length);
                if (read <= 0)
                    throw new IOException("Error reading Ogg Vorbis samples");
                readOffset += read;

                if (readOffset > vorbis.TotalSamples)
                {
                    readOffset = 0;
                    vorbis.DecodedPosition = 0;
                }

                //multithread?

                //convert to 16 bit PCM
                for (int i = 0; i < read; ++i)
                {
                    var n = (short)(intermediate[i] * 0x8000);
                    samples[i * 2] = (byte)(n & 0xff);
                    samples[i * 2 + 1] = (byte)((n >> 8) & 0xff);
                }

                instance.SubmitBuffer(samples);
            }

            public void Dispose()
            {
                vorbis.Dispose();
            }
        }

        class OggLoader : CustomLoader
        {
            public override bool PreserveStream => true;

            //todo: use https://github.com/flibitijibibo/Vorbisfile-CS/blob/24e9ece0239da6b03890d47f6118df85d52cf179/Vorbisfile.cs

            public override object Load(CustomLoad load)
            {
                var vorbis = new NVorbis.VorbisReader(load.stream, true);
                if (vorbis.Channels < 1 || vorbis.Channels > 2)
                    throw new FormatException($"Audio must be in mono or stero (provided: {vorbis.Channels})");

                if (vorbis.SampleRate < 8000 || vorbis.SampleRate > 48000)
                    throw new FormatException($"Audio must be between 8KHz and 48KHz (provided: {vorbis.SampleRate / 1024:N1}KHz");

                return new OggSoundSource(vorbis);
            }
        }


        class SoundLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                var sound = new SoundEffectSoundSource
                {
                    sound = SoundEffect.FromStream(load.stream)
                };
                sound.sound.Name = load.name;
                return sound;
            }
        }

        class EffectLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                var bytes = new byte[load.length];
                load.stream.Read(bytes, 0, bytes.Length);

                //load = TransformPath(file, DataFolder, "Shaders", "DX11");
                return new Effect(Runtime.GraphicsDevice, bytes)
                {
                    Name = load.name
                };
            }
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

        #endregion
    }
}