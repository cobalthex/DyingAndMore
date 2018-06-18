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
        /// <summary>
        /// Where all content lives
        /// </summary>
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
            CustomLoaders.Add("png", loadTex);
            CustomLoaders.Add("jpg", loadTex);
            CustomLoaders.Add("jpeg", loadTex);
            CustomLoaders.Add("tga", loadTex);
            CustomLoaders.Add("tiff", loadTex);
            CustomLoaders.Add("bmp", loadTex);
            CustomLoaders.Add("gif", loadTex);

            CustomLoaders.Add("bfnt", new BitmapFontLoader());

            CustomLoaders.Add("ogg", new UnsupportedLoader());
            CustomLoaders.Add("opus", new OpusLoader());
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

        //todo: somehow provide ability to load files from zip without loading in as zip/...

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

            //var swatch = System.Diagnostics.Stopwatch.StartNew();

            if (root == null)
                root = DefaultRoot;
            else if (root == "")
                root = Directory.GetCurrentDirectory();

            root = Normalize(root);
            file = Normalize(file);

            string realFile;
            if (PathStartsWith(file, DefaultRoot) && root == DefaultRoot)
            {
                realFile = file;
                file = file.Substring(DefaultRoot.Length + 1);
            }
            else if (Path.IsPathRooted(file) || PathStartsWith(file, root))
            {
                //normalize full paths in the root directory
                var rootFullPath = Normalize(Path.GetFullPath(root));
                if (file.StartsWith(rootFullPath))
                {
                    file = file.Substring(rootFullPath.Length + (rootFullPath.EndsWith("/") ? 0 : 1));
                    realFile = root + "/" + file;
                }
                else
                    realFile = file;
            }
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
                string fileRefName = file;

                int zipStrIndex = file.IndexOf(".zip");
                //load a file from inside a zip (archive.zip/test.file)
                if (zipStrIndex >= 0 && zipStrIndex < file.Length - 5 && file[zipStrIndex + 4] == '/')
                {
                    fileRefName = file;
                    root = Path.Combine(root, file.Substring(0, zipStrIndex + 4));
                    file = file.Substring(zipStrIndex + 5);
                }

                ZipArchive zip = null;
                bool openedZip = false;
                if (root.EndsWith(".zip") && !openZips.TryGetValue(root, out zip))
                {
                    //recursive zip loading not supported (yet)
                    zip = new ZipArchive(File.OpenRead(root), ZipArchiveMode.Read, false);
                    openZips[root] = zip;
                    openedZip = true;
                }

                var load = new CustomLoad { name = file, file = realFile };
                if (zip != null)
                {
                    var entry = zip.GetEntry(file);
                    if (entry == null)
                        return Load(file, null, forceLoad); //try loading from default location

                    load.length = entry.Length;

                    //zip streams do not support seeking
                    var memStream = new MemoryStream(); //lazy memory stream? (load only up to bytes requested)
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
                            sxt.File = fileRefName;
                        load.stream.Dispose();
                    }
                }
                catch
                {
                    load.stream.Dispose();
                    lateLoads.Remove(realFile);
                    throw;
                }
                finally
                {
                    if (openedZip)
                        zip.Dispose();
                }
            }

            if (obj.reference == null)
            {
                if (lateLoads.Count > 0)
                    throw new FileNotFoundException("Could not find referenced file(s): " + String.Join(", ", lateLoads.Keys));
                else
                    throw new Exception("Wtf loading " + realFile); //todo
            }

            if (forceLoad && exists)
            {
                var target = objects[realFile].reference.Target;
                if (target is Texture2D texDst &&
                    obj.reference.Target is Texture2D texSrc)
                {
                    UInt32[] texData = new UInt32[texSrc.Width * texSrc.Height];
                    texSrc.GetData(texData);
                    texDst.SetData(texData);
                    texSrc.Dispose();
                }
                else
                {
                    //todo: specific option for applying rather than replacing (and maybe serializer.get_dictionary)
                    Serializer.ApplyObject(target, Serializer.Cast(objects[realFile].reference.Target.GetType(), obj.reference.Target));
                }
                obj = objects[realFile];
            }
            else
                objects[realFile] = obj;

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
            using (var writer = new StreamWriter(new FileStream(file, FileMode.Create)))
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

        public static void ReloadAll(bool discardNotFound = false)
        {
            var removed = new List<string>();
            foreach (var obj in objects)
            {
                //todo: parse path to extract root
                try
                {
                    Load(obj.Key, null, true);
                }
                catch
                {
                    if (discardNotFound)
                        removed.Add(obj.Key);
                }
            }
            foreach (var remove in removed)
                objects.Remove(remove);
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

#if WINDOWS
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
            var path = Normalize(e.FullPath);

            //this may be called multiple times: https://blogs.msdn.microsoft.com/oldnewthing/20140507-00/?p=1053/
            System.Threading.Thread.Sleep(500);
            try
            {
                if (objects.ContainsKey(path))
                {
                    Load(path, null, true);
                    LogBuffer.Append("Refreshed " + e.FullPath);
                }
            }
            catch (Exception ex)
            {
                //todo: nested references?
                lateLoads.Remove(path);
                LogBuffer.Append($"Failed to refresh {e.FullPath} ({ex.Message})");
            }
        }
#elif WINDOWS_UAP
        public static void WatchDirectory(string directory, string filter = "*.*")
        {
            //todo
        }
#endif


        #region Custom Loaders

        class UnsupportedLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                throw new NotSupportedException($"Reading from {Path.GetExtension(load.file)} is not supported");
            }
        }

        class ZipLoader : CustomLoader
        {
            public override object Load(CustomLoad load)
            {
                Dictionary<string, object> loadedObjects = new Dictionary<string, object>();

                bool openedZip = false;
                if (!openZips.TryGetValue(load.file, out var zip))
                {
                    openedZip = true;
                    zip = new ZipArchive(load.stream, ZipArchiveMode.Read, false);
                    openZips.Add(load.file, zip);
                }

                foreach (var entry in zip.Entries)
                    loadedObjects[entry.FullName] = Cache.Load(entry.FullName, load.file, false);

                if (openedZip)
                {
                    openZips.Remove(load.file);
                    zip.Dispose();
                }

                return loadedObjects;
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

        public class OpusSoundSource : ISoundSource
        {
            Concentus.Structs.OpusDecoder decoder;
            Concentus.Oggfile.OpusOggReadStream stream;

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
            struct UnionArray
            {
                [System.Runtime.InteropServices.FieldOffset(0)]
                public Byte[] Bytes;

                [System.Runtime.InteropServices.FieldOffset(0)]
                public short[] Shorts;
            }

            //assumes single threaded
            UnionArray samples;

            public OpusSoundSource(Concentus.Oggfile.OpusOggReadStream stream, Concentus.Structs.OpusDecoder decoder)
            {
                this.stream = stream;
                this.decoder = decoder;
                //todo: something is fucky here (leak)
            }

            public SoundEffectInstance Instantiate()
            {
                if (stream == null)
                    return new DynamicSoundEffectInstance(22500, AudioChannels.Mono); //todo

                var instance = new DynamicSoundEffectInstance(decoder.SampleRate, (AudioChannels)decoder.NumChannels);
                instance.BufferNeeded += Instance_BufferNeeded;
                Instance_BufferNeeded(instance, EventArgs.Empty); //preload first buffer
                return instance;
            }

            private void Instance_BufferNeeded(object sender, EventArgs e)
            {
                var instance = (DynamicSoundEffectInstance)sender;

                if (stream.HasNextPacket)
                {
                    samples.Shorts = stream.DecodeNextPacket();
                    if (samples.Shorts == null)
                        return; //todo: end of stream or error
                }

                //multithread?

                instance.SubmitBuffer(samples.Bytes);
            }

            public void Dispose()
            {
                decoder.ResetState();
            }
        }

        class OpusLoader : CustomLoader
        {
            public override bool PreserveStream => true;

            //todo: use for vorbis: https://github.com/flibitijibibo/Vorbisfile-CS/blob/24e9ece0239da6b03890d47f6118df85d52cf179/Vorbisfile.cs


            //opus decoder (not vorbis)

            public override object Load(CustomLoad load)
            {
                var decoder = new Concentus.Structs.OpusDecoder(48000, 2); //todo: configurable quality?
                var oggStream = new Concentus.Oggfile.OpusOggReadStream(decoder, load.stream);
                return new OpusSoundSource(oggStream, decoder);
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

        #endregion
    }
}