using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Takai.Data
{
    /// <summary>
    /// A store for all deserialized files
    /// </summary>
    public static class Cache
    {
        public static ReadOnlyDictionary<string, object> Objects { get; private set; }
        private static Dictionary<string, object> objects;

        static Cache()
        {
            objects = new Dictionary<string, object>();
            Objects = new ReadOnlyDictionary<string, object>(objects);
        }

        public static object Load(string file, bool forceLoad = false)
        {
            file = Normalize(file);
            if (forceLoad || !objects.TryGetValue(file, out var obj))
            {
                //todo: support data files too
                obj = Serializer.TextDeserialize(file);
                if (obj is ISerializeExternally sxt)
                    sxt.File = file;
                objects[file] = obj;
            }
            return obj;
        }

        public static T Load<T>(string file, bool forceLoad = false)
        {
            file = Normalize(file);
            if (forceLoad || !objects.TryGetValue(file, out var obj))
            {
                obj = Serializer.TextDeserialize<T>(file);
                if (obj is ISerializeExternally sxt)
                    sxt.File = file;
                objects[file] = obj;
            }
            return (T)obj;
        }

        /// <summary>
        /// Reload all known defs from their files. Any objects that fail to reload will remain the same
        /// </summary>
        /// <returns>The objects that were reloaded</returns>
        public static Dictionary<string, object> ReloadAll()
        {
            var newObjs = new Dictionary<string, object>();

            foreach (var file in objects)
            {
                try
                {
                    var obj = Serializer.TextDeserialize(file.Key);
                    if (obj is ISerializeExternally sxt)
                        sxt.File = file.Key;
                    newObjs[file.Key] = obj;
                }
                catch { }
            }

            foreach (var obj in newObjs)
                objects[obj.Key] = obj.Value;

            return newObjs;
        }

        public static void SaveAllToFile(string file)
        {
            using (var writer = new StreamWriter(file))
                Serializer.TextSerialize(writer, Objects);

            var asdf = Serializer.TextDeserialize(file);
        }

        public static string Normalize(string path)
        {
            return path.Replace("\\\\", "/").Replace('\\', '/');
        }
    }
}
