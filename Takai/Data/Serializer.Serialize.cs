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
    /// This member/object should be serizlied with the specified method
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomSerializeAttribute : System.Attribute
    {
        internal MethodInfo Serialize;

        public CustomSerializeAttribute(Type Type, string MethodName, bool IsStatic = true)
        {
            var method = Type.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | (IsStatic ? BindingFlags.Static : 0));
            this.Serialize = method;
        }
    }

    public static partial class Serializer
    {

        /// <summary>
        /// Serialize an object to a text stream
        /// </summary>
        /// <param name="Stream">The stream to write to</param>
        /// <param name="Object">The object to serialize</param>
        /// <remarks>Some data types (Takai/Xna) are custom serialized</remarks>
        public static void TextSerialize(StreamWriter Stream, object Object, int IndentLevel = 0)
        {
            if (Object == null)
            {
                Stream.Write("Null");
                return;
            }

            var ty = Object.GetType();
            var custSerial = ty.GetCustomAttribute<CustomSerializeAttribute>(false)?.Serialize;

            if (ty.IsPrimitive)
                Stream.Write(Object);

            else if (ty.IsEnum) //todo: flags
            {
                Stream.Write(WriteFullTypeNames ? ty.FullName : ty.Name);

                if (Attribute.IsDefined(ty, typeof(FlagsAttribute)) && Convert.ToInt32(Object) != 0)
                {
                    var e = Object as Enum;
                    foreach (Enum flag in Enum.GetValues(ty))
                        if (Convert.ToInt32(flag) != 0 && e.HasFlag(flag))
                            Stream.Write(" {0}", flag.ToString());
                }
                else
                    Stream.Write(" {0}", Object.ToString());
            }

            else if (ty == typeof(string) || ty == typeof(char[]))
                Stream.Write(Object.ToString().ToLiteral());

            //user-defined serializer
            else if (custSerial != null)
                TextSerialize(Stream, custSerial.Invoke(null, new[] { Object }), IndentLevel);

            //custom serializer
            else if (Serializers.ContainsKey(ty) && Serializers[ty].Serialize != null)
                TextSerialize(Stream, Serializers[ty].Serialize(Object), IndentLevel);

            else if (typeof(IDictionary).IsAssignableFrom(ty))
            {
                Stream.WriteLine("{");

                var dict = (IDictionary)Object;
                foreach (var prop in dict.Keys)
                {
                    Indent(Stream, IndentLevel + 1);
                    Stream.Write("{0}: ", prop.ToString());
                    TextSerialize(Stream, dict[prop], IndentLevel + 1);
                    Stream.WriteLine(";");
                }

                Indent(Stream, IndentLevel);
                Stream.Write("}");
            }

            else if (typeof(IEnumerable).IsAssignableFrom(ty))
            {
                Stream.Write("[");
                bool once = false, lastWasSerializer = false;
                foreach (var i in (IEnumerable)Object)
                {
                    lastWasSerializer = false;
                    if (!i.GetType().IsPrimitive)
                    {
                        Stream.WriteLine();
                        Indent(Stream, IndentLevel + 1);
                        lastWasSerializer = true;
                    }
                    else if (once)
                        Stream.Write(' ');
                    once = true;

                    TextSerialize(Stream, i, IndentLevel + 1);
                }
                if (lastWasSerializer)
                {
                    Stream.WriteLine();
                    Indent(Stream, IndentLevel);
                }
                Stream.Write("]");
            }

            else
            {
                Stream.WriteLine("{0} {{", WriteFullTypeNames ? ty.FullName : ty.Name);

                foreach (var field in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    SerializeMember(Stream, field, field.GetValue(Object), IndentLevel);

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    SerializeMember(Stream, field, field.GetValue(Object), IndentLevel);

                Indent(Stream, IndentLevel);
                Stream.Write("}");
            }
        }

        private static void SerializeMember(StreamWriter Stream, MemberInfo Member, object Value, int IndentLevel)
        {
            if (Member.GetCustomAttribute<Data.NonSerializedAttribute>() != null)
                return;

            Indent(Stream, IndentLevel + 1);
            Stream.Write("{0}: ", Member.Name);

            //user-defined serializer
            var attr = Member.GetCustomAttribute<CustomSerializeAttribute>()?.Serialize;
            if (attr != null)
                TextSerialize(Stream, attr.Invoke(null, new[] { Value }), IndentLevel + 1);
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
