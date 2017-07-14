using System;
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
                objects.Add(file, obj);
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
                objects.Add(file, obj);
            }
            return (T)obj;
        }

        public static void SaveAllToFile(string file)
        {
            using (var writer = new System.IO.StreamWriter(file))
                Serializer.TextSerialize(writer, Objects);

            var asdf = Serializer.TextDeserialize(file);
        }

        public static string Normalize(string path)
        {
            return path.Replace("\\\\", "/").Replace('\\', '/');
        }
    }
}
