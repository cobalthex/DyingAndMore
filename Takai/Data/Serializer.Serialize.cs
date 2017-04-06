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
    /// This member/object should be serizlied with the specified (static) method
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomSerializeAttribute : Attribute
    {
        internal MethodInfo serialize;

        /// <summary>
        /// Create a custom serializer
        /// </summary>
        /// <param name="Type">The type containing the method to use for serializing</param>
        /// <param name="MethodName">The name of the method</param>
        /// <param name="OverrideSerializeType">If the object returned is a dictionary, export it as type <see cref="Type"/></param>
        public CustomSerializeAttribute(Type Type, string MethodName)
        {
            var method = Type.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            serialize = method;
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
        public static void TextSerialize(string file, object serializing)
        {
            using (var writer = new StreamWriter(file))
                TextSerialize(writer, serializing);
        }

        /// <summary>
        /// Serialize an object to a text stream
        /// </summary>
        /// <param name="writer">The stream to write to</param>
        /// <param name="serializing">The object to serialize</param>
        /// <remarks>Some data types (Takai/Xna) are custom serialized</remarks>
        public static void TextSerialize(StreamWriter writer, object serializing, int indentLevel = 0)
        {
            if (serializing == null)
            {
                writer.Write("Null");
                return;
            }

            var ty = serializing.GetType();
            var custSerial = ty.GetCustomAttribute<CustomSerializeAttribute>(false);

            if (ty.IsPrimitive)
                writer.Write(serializing);

            else if (ty.IsEnum)
            {
                writer.Write(WriteFullTypeNames ? ty.FullName : ty.Name);

                writer.Write(" [");
                if (Attribute.IsDefined(ty, typeof(FlagsAttribute)) && Convert.ToUInt64(serializing) != 0)
                {
                    var e = serializing as Enum;
                    var enumValues = Enum.GetValues(ty);
                    foreach (Enum flag in enumValues)
                    {
                        if (Convert.ToUInt64(flag) != 0 && e.HasFlag(flag))
                            writer.Write(" {0}", flag.ToString());
                    }
                }
                else
                    writer.Write(" {0}", serializing.ToString());
                writer.Write(" ]");
            }

            else if (ty == typeof(string) || ty == typeof(char[]))
                writer.Write(serializing.ToString().ToLiteral());

            //user-defined serializer
            else if (custSerial?.serialize != null)
            {
                var serialized = custSerial.serialize.Invoke(null, new[] { serializing });
                TextSerialize(writer, serialized, indentLevel);
            }

            //todo: maybe remove and just add a bunch of custom serializers
            //custom serializer
            else if (Serializers.ContainsKey(ty) && Serializers[ty].Serialize != null)
                TextSerialize(writer, Serializers[ty].Serialize(serializing), indentLevel);

            else if (typeof(IDictionary).IsAssignableFrom(ty))
            {
                writer.WriteLine("{");

                var dict = (IDictionary)serializing;
                var keyType = ty.GetGenericArguments()[0];
                foreach (var prop in dict.Keys)
                {
                    Indent(writer, indentLevel + 1);

                    if (keyType == typeof(char))
                        writer.Write((UInt16)(char)prop);
                    else
                        writer.Write(prop.ToString());

                    writer.Write(": ");
                    TextSerialize(writer, dict[prop], indentLevel + 1);
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
                    if (!i.GetType().IsPrimitive)
                    {
                        writer.WriteLine();
                        Indent(writer, indentLevel + 1);
                        lastWasSerializer = true;
                    }
                    else if (once)
                        writer.Write(' ');
                    once = true;

                    TextSerialize(writer, i, indentLevel + 1);
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

                foreach (var field in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    SerializeMember(writer, field, field.GetValue(serializing), indentLevel);

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    SerializeMember(writer, field, field.GetValue(serializing), indentLevel);

                //derived values
                var derived = ty.GetCustomAttribute<DerivedTypeSerializeAttribute>();
                if (derived != null)
                {
                    var props = (Dictionary<string, object>)derived.serialize.Invoke(serializing, null);

                    foreach (var prop in props)
                    {
                        Indent(writer, indentLevel + 1);
                        writer.Write("{0}: ", prop.Key);
                        TextSerialize(writer, prop.Value, indentLevel + 1);
                        writer.WriteLine(";");
                    }
                }

                Indent(writer, indentLevel);
                writer.Write("}");
            }
        }

        private static void SerializeMember(StreamWriter Stream, MemberInfo Member, object Value, int IndentLevel)
        {
            if (Member.GetCustomAttribute<Data.Serializer.IgnoredAttribute>() != null)
                return;

            Indent(Stream, IndentLevel + 1);
            Stream.Write("{0}: ", Member.Name);

            //user-defined serializer
            var custSerial = Member.GetCustomAttribute<CustomSerializeAttribute>();
            if (custSerial?.serialize != null)
                TextSerialize(Stream, custSerial.serialize.Invoke(null, new[] { Value }), IndentLevel + 1);
            //normal serializer
            else
                TextSerialize(Stream, Value, IndentLevel + 1);

            Stream.WriteLine(";");
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
