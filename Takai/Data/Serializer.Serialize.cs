using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Data
{
    /// <summary>
    /// This member/object should be serizlied with the specified (instance) method
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomSerializeAttribute : Attribute
    {
        public const BindingFlags Flags = 0
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.IgnoreCase;

        internal string methodName;

        /// <summary>
        /// Create a custom serializer
        /// </summary>
        /// <param name="Type">The type containing the method to use for serializing</param>
        /// <param name="MethodName">The name of the method</param>
        /// <param name="OverrideSerializeType">If the object returned is a dictionary, export it as type <see cref="Type"/></param>
        public CustomSerializeAttribute(string methodName)
        {
            this.methodName = methodName;
        }
    }

    /// <summary>
    /// The object should use the specified method to add custom fields to the serialized object
    /// Useful for adding aggregate/derived data to a serialized type
    /// </summary>
    /// <remarks>Only used if a typed object is serialized (dictionaries/primatives ignored)</remarks>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DerivedTypeSerializeAttribute : Attribute
    {
        internal MethodInfo serialize;

        /// <summary>
        /// Create a derived type serializer
        /// </summary>
        /// <param name="type">The object to look in for the serialize method</param>
        /// <param name="methodName">The name of the method to search for (Must be non-static, can be public or non public)</param>
        /// <remarks>format: Dictionary&lt;string, object&gt;(object Source)</remarks>
        public DerivedTypeSerializeAttribute(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            serialize = method;
        }
    }

    public static partial class Serializer
    {
        /// <summary>
        /// Specifies that a custom serializer should serialize the object to an ordered array of all members
        /// </summary>
        public static readonly object LinearStruct = new object();

        public static void TextSerialize(string file, object serializing)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            using (var writer = new StreamWriter(file))
                TextSerialize(writer, serializing);
        }

        /// <summary>
        /// Serialize an object to a text stream
        /// </summary>
        /// <param name="writer">The stream to write to</param>
        /// <param name="serializing">The object to serialize</param>
        /// <remarks>Some data types (Takai/Xna) are custom serialized</remarks>
        public static void TextSerialize(StreamWriter writer, object serializing, int indentLevel = 0, bool serializeExternals = false)
        {
            if (serializing == null)
            {
                writer.Write("Null");
                return;
            }

            var ty = serializing.GetType();

            //external serialization
            if (!serializeExternals && typeof(ISerializeExternally).IsAssignableFrom(ty))
            {
                var fileProp = ty.GetProperty("File");
                if (fileProp != null)
                {
                    var fileValue = (string)fileProp.GetValue(serializing);
                    if (fileValue != null)
                    {
                        writer.Write($"@\"{fileValue}\"");
                        return;
                    }
                }
            }

            var custSerial = ty.GetCustomAttribute<CustomSerializeAttribute>(true);

            if (ty.IsPrimitive)
                writer.Write(serializing);

            else if (ty.IsEnum)
            {
                writer.Write(WriteFullTypeNames ? ty.FullName : ty.Name);

                if (Attribute.IsDefined(ty, typeof(FlagsAttribute)) && Convert.ToUInt64(serializing) != 0)
                {
                    writer.Write(" [");
                    var e = serializing as Enum;
                    var enumValues = Enum.GetNames(ty);
                    int n = 0;
                    foreach (var flag in enumValues)
                    {
                        var value = (Enum)Enum.Parse(ty, flag);
                        if (Convert.ToUInt64(value) != 0 && e.HasFlag(value))
                        {
                            if (n++ > 0)
                                writer.Write(' ');
                            writer.Write(flag);
                        }
                    }
                    writer.Write("]");
                }
                else
                    writer.Write(".{0}", serializing.ToString());
            }

            else if (ty == typeof(string) || ty == typeof(char[]))
                writer.Write(serializing.ToString().ToLiteral());

            //user-defined serializer
            else if (custSerial?.methodName != null)
            {
                var method = ty.GetMethod(custSerial.methodName, CustomSerializeAttribute.Flags);
                var serialized = method?.Invoke(serializing, null);
                TextSerialize(writer, serialized, indentLevel, serializeExternals);
            }

            //todo: maybe remove and just add a bunch of custom serializers
            //custom serializer
            else if (Serializers.ContainsKey(ty) && Serializers[ty].Serialize != null)
            {
                var serialized = Serializers[ty].Serialize(serializing);
                if (serialized == LinearStruct)
                    SerializeLinear(writer, serializing, serializeExternals);
                else
                    TextSerialize(writer, serialized, indentLevel, serializeExternals);
            }

            else if (typeof(IDictionary).IsAssignableFrom(ty))
            {
                writer.WriteLine("{");

                var dict = (IDictionary)serializing;
                var keyType = ty.GetGenericArguments()[0];
                foreach (var key in dict.Keys)
                {
                    Indent(writer, indentLevel + 1);

                    if (keyType == typeof(char))
                        writer.Write((UInt16)(char)key);
                    else
                        writer.Write(key.ToString());

                    writer.Write(": ");
                    TextSerialize(writer, dict[key], indentLevel + 1, serializeExternals);
                    writer.WriteLine(";");
                }

                Indent(writer, indentLevel);
                writer.Write("}");
            }

            else if (typeof(IEnumerable).IsAssignableFrom(ty))
            {
                writer.Write("[");
                bool once = false, lastWasSerializer = false;
                foreach (var i in (IEnumerable)serializing)
                {
                    lastWasSerializer = false;
                    if (i == null || !i.GetType().IsPrimitive)
                    {
                        writer.WriteLine();
                        Indent(writer, indentLevel + 1);
                        lastWasSerializer = true;
                    }
                    else if (once)
                        writer.Write(' ');
                    once = true;

                    TextSerialize(writer, i, indentLevel + 1, serializeExternals);
                }
                if (lastWasSerializer)
                {
                    writer.WriteLine();
                    Indent(writer, indentLevel);
                }
                writer.Write("]");
            }

            else
            {
                writer.WriteLine($"{(WriteFullTypeNames ? ty.FullName : ty.Name)} {{");

                foreach (var prop in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    SerializeMember(writer, serializing, prop, prop.GetValue(serializing), indentLevel, serializeExternals);

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    SerializeMember(writer, serializing, field, field.GetValue(serializing), indentLevel, serializeExternals);

                //derived values
                var derived = ty.GetCustomAttribute<DerivedTypeSerializeAttribute>(true);
                if (derived != null)
                {
                    var props = (Dictionary<string, object>)derived.serialize.Invoke(serializing, null);

                    foreach (var prop in props)
                    {
                        Indent(writer, indentLevel + 1);
                        writer.Write("{0}: ", prop.Key);
                        TextSerialize(writer, prop.Value, indentLevel + 1, serializeExternals);
                        writer.WriteLine(";");
                    }
                }

                Indent(writer, indentLevel);
                writer.Write("}");
            }
        }

        private static void SerializeMember(StreamWriter writer, object parent, MemberInfo member, object value, int indentLevel, bool serializeExternals)
        {
            if (member.GetCustomAttribute<IgnoredAttribute>(true) != null)
                return;

            Indent(writer, indentLevel + 1);
            writer.Write("{0}: ", member.Name);

            if (value == null)
            {
                writer.WriteLine("Null;");
                return;
            }

            //custom-defined serializer
            var custSerial = member.GetCustomAttribute<CustomSerializeAttribute>(true);
            if (custSerial?.methodName != null)
            {
                var method = member.DeclaringType.GetMethod(custSerial.methodName, CustomSerializeAttribute.Flags);
                TextSerialize(writer, method.Invoke(parent, null), indentLevel + 1, serializeExternals);
            }
            else
                TextSerialize(writer, value, indentLevel + 1, serializeExternals);

            writer.WriteLine(";");
        }

        private static void SerializeLinear(StreamWriter writer, object serializing, bool serializeExternals)
        {
            writer.Write('[');
            var ty = serializing.GetType();
            int n = 0;
            foreach (var member in ty.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        if (n++ > 0)
                            writer.Write(' ');

                        TextSerialize(writer, ((FieldInfo)member).GetValue(serializing), 0, serializeExternals);
                        break;
                    case MemberTypes.Property:
                        if (n++ > 0)
                            writer.Write(' ');

                        TextSerialize(writer, ((PropertyInfo)member).GetValue(serializing), 0, serializeExternals);
                        break;
                }
            }
            writer.Write(']');
        }

        private static void Indent(StreamWriter Stream, int IndentLevel)
        {
            Stream.Write(new string(' ', IndentLevel * 4));
        }

        private static string ToLiteral(this string Input)
        {
            var literal = new StringBuilder(Input.Length + 2);
            literal.Append("\"");
            foreach (var c in Input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        literal.Append(c);
                        break;
                }
            }
            literal.Append("\"");
            return literal.ToString();
        }
    }
}
