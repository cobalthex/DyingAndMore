using System;
using System.Collections.Generic;
using System.IO;

namespace Takai.Data
{
    public static partial class Serializer
    {
        internal enum BinSerizlieEntryType : byte
        {
            Unknown = 0,
            Pointer, //points to an entry
            ExternalReference,
            InternalReference,
            Type,
            Null,
            Bool,
            Int, //or char
            Float,
            String,
            KeyValue,
            List,
        }

        public static void BinSerialize(string file, object serializing)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            using (var writer = new BinaryWriter(new FileStream(file, FileMode.Create)))
                BinSerialize(writer, serializing);
        }

        public static void BinSerialize(BinaryWriter writer, object serializing)
        {
            uint idCounter = 0;
            var known = new Dictionary<uint, object>();

            writer.Write(new[] { 'T', 'K', '0', '1' }); //magic + version
            //checksum? (at end?)



            //external refs
            //internal refs
            //nested objects in reverse order
        }
    }
}
