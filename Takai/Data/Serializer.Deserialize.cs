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
    public class CustomDeserializeAttribute : System.Attribute
    {
        internal MethodInfo Deserialize;

        public CustomDeserializeAttribute(Type Type, string MethodName, bool IsStatic = true)
        {
            var method = Type.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | (IsStatic ? BindingFlags.Static : 0));
            this.Deserialize = method;
        } 
    }

    public static partial class Serializer
    {
        private static readonly MethodInfo CastMethod = typeof(Enumerable).GetMethod("Cast");
        private static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethod("ToList");
        private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

        /// <summary>
        /// Desearialize an object from a text file (In the same format created by TextSerialize)
        /// </summary>
        /// <param name="Stream">The stream to read from</param>
        /// <returns>The object created</returns>
        /// <remarks>
        /// Return types:
        ///     [] - List&lt;object&gt;
        ///     "string" - string
        ///     'string' - string
        ///     0.000 - float (or double if too large)
        ///     00000 - int (or long if too large)
        ///     true/false - bool
        ///     null - null
        ///     Type prop1 prop2 - Enum
        ///     Type {...} - Type
        ///     {...} - Dictionary&lt;string, object&gt; -- all keys converte to lowercase if CaseSensitiveMembers is false
        /// </remarks>
        public static object TextDeserialize(StreamReader Stream)
        {
            //todo: comments
            //todo: allow ; or , in arrays

            SkipWhitespace(Stream);

            var pk = (char)Stream.Peek();
            if (pk == ';')
                return null;
            //strings
            if (pk == '"' || pk == '\'')
                return ReadString(Stream);
            //numbers
            if (char.IsDigit(pk) || pk == '-' || pk == '.' || pk == ',')
            {
                var num = ReadWord(Stream);
                if (num.Contains(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
                {
                    try { return float.Parse(num); }
                    catch (OverflowException) { return double.Parse(num); }
                }
                try { return int.Parse(num); }
                catch (OverflowException) { return long.Parse(num); }
            }
            //arrays
            if (pk == '[')
            {
                Stream.Read();
                SkipWhitespace(Stream);

                var list = new List<object>();
                while (!Stream.EndOfStream && Stream.Peek() != ']')
                {
                    list.Add(TextDeserialize(Stream));
                    SkipWhitespace(Stream);
                }
                Stream.Read();
                return list;
            }
            //dict
            if (pk == '{')
            {
                Stream.Read();
                SkipWhitespace(Stream);

                var d = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
                while (!Stream.EndOfStream && Stream.Peek() != '}')
                {
                    var name = ReadWord(Stream).ToLower();
                    if (Stream.Read() != ':')
                        throw new FormatException("Format is name: value;");

                    SkipWhitespace(Stream);
                    var value = TextDeserialize(Stream);
                    
                    d[name] = value;

                    SkipWhitespace(Stream);
                    pk = (char)Stream.Peek();
                    if (pk == ';')
                    {
                        Stream.Read();
                        SkipWhitespace(Stream);
                    }
                    else if (pk != '}')
                        throw new FormatException(string.Format("Missing semicolon for property '{0}'", name));
                }

                Stream.Read();
                return d;
            }

            var word = ReadWord(Stream);
            var lword = word.ToLower();
            SkipWhitespace(Stream);

            if (lword == "true")
                return true;
            if (lword == "false")
                return false;
            if (lword == "null")
                return null;

            System.Type ty;
            if (!RegisteredTypes.TryGetValue(word, out ty))
            {
                throw new FormatException("Type not found: " + word); //todo: add requested type and property
                //return null;
            }

            //enum
            if (ty.IsEnum)
            {

                ulong values = 0;

                do
                {
                    var value = ReadWord(Stream);
                    values |= Convert.ToUInt64(Enum.Parse(ty, value));

                    SkipWhitespace(Stream);
                } while (!IsTerminator((char)Stream.Peek()));

                return Enum.ToObject(ty, values);
            }

            var dict = TextDeserialize(Stream) as Dictionary<string, object>;

            if (dict != null)
            {
                var inst = Activator.CreateInstance(ty);

                foreach (var field in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance | (CaseSensitiveMembers ? 0 : BindingFlags.IgnoreCase)))
                {
                    object val;
                    if (DeserializeField(field, field.PropertyType, dict, out val))
                        field.SetValue(inst, val);
                }

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance | (CaseSensitiveMembers ? 0 : BindingFlags.IgnoreCase)))
                {
                    object val;
                    if (DeserializeField(field, field.FieldType, dict, out val))
                        field.SetValue(inst, val);
                }

                return inst;
            }

            return null;
        }

        //todo: use System.ComponentModel.TypeDescriptor.GetConverter (and ConvertFromString)

        private static bool DeserializeField(MemberInfo Member, Type Type, Dictionary<string, object> Props, out object Value)
        {
            //ignored
            if (Member.GetCustomAttribute<Data.NonSerializedAttribute>() != null)
            {
                Value = null;
                return false;
            }

            object prop;

            //not set
            if (!Props.TryGetValue(Member.Name, out prop))
            {
                Value = null;
                return false;
            }

            if (prop == null)
            {
                Value = prop;
                return true;
            }

            //user defined serializers
            var deserial = Member.GetCustomAttribute<CustomDeserializeAttribute>() ?? Type.GetCustomAttribute<CustomDeserializeAttribute>();
            if (deserial != null)
            {
                Value = deserial.Deserialize.Invoke(null, new[] { prop });
                return true;
            }

            //custom serializers
            if (Serializers.ContainsKey(Type) && Serializers[Type].Deserialize != null)
            {
                Value = Serializers[Type].Deserialize(prop);
                return true;
            }

            if (prop is IList)
            {
                var ety = Type.HasElementType ? Type.GetElementType() : Type.GetGenericArguments()[0];
                var orig = (List<object>)prop;

                try
                {
                    var casted = CastMethod.MakeGenericMethod(ety).Invoke(null, new[] { orig });

                    //exception only happens during ToArray/List
                    if (Type.IsArray)
                        Value = ToArrayMethod.MakeGenericMethod(ety).Invoke(null, new[] { casted });
                    else
                        Value = ToListMethod.MakeGenericMethod(ety).Invoke(null, new[] { casted });
                }
                catch
                {
                    var cvt = orig.Select(x => Convert.ChangeType(x, ety));
                    var casted = CastMethod.MakeGenericMethod(ety).Invoke(null, new[] { cvt });

                    if (Type.IsArray)
                        Value = ToArrayMethod.MakeGenericMethod(ety).Invoke(null, new[] { casted });
                    else
                        Value = ToListMethod.MakeGenericMethod(ety).Invoke(null, new[] { casted });
                }

                return true;
            }

            if (prop is IDictionary)
            {
                var gArgs = Type.GetGenericArguments(); //[key, value]
                var orig = (Dictionary<string, object>)prop;

                var converter = System.ComponentModel.TypeDescriptor.GetConverter(gArgs[0]);
                if (converter != null)
                {
                    var dict = (IDictionary)Activator.CreateInstance(Type);

                    foreach (var kv in orig)
                    {
                        try
                        {
                            object key;

                            if (gArgs[0] == typeof(string))
                                key = kv.Key;
                            else if (gArgs[0].IsEnum)
                                key = Enum.Parse(gArgs[0], kv.Key, true);
                            else
                                key = converter.ConvertFrom(kv.Key);

                            object val;
                            EvaluateAs(gArgs[1], kv.Value, out val);

                            dict.Add(key, val);
                        }
                        catch { }
                    }

                    Value = dict;
                    return true;
                }

                Value = null;
                return false;
            }

            Value = prop;
            return true;
        }

        /// <summary>
        /// Attempt to deserialize an intermediate object as a specific type (Typically used when reading from dictionaries)
        /// </summary>
        /// <param name="Type">The type to deserialize as</param>
        /// <param name="Source">The intermediate object to deserialize</param>
        /// <param name="Value">The value deserialized</param>
        /// <returns>True if the object was deserialized to Value, false if the type is unknown to the deserializer</returns>
        /// <remarks>If Intermediate type and T do not match, will likely throw an exception</remarks>
        public static bool EvaluateAs(Type Type, object Source, out object Value)
        {
            //user defined serializers
            var deserial = Type.GetCustomAttribute<CustomDeserializeAttribute>();
            if (deserial != null)
            {
                Value = deserial.Deserialize.Invoke(null, new[] { Source });
                return true;
            }

            //custom serializers
            if (Serializers.ContainsKey(Type) && Serializers[Type].Deserialize != null)
            {
                Value = Serializers[Type].Deserialize(Source);
                return true;
            }

            Value = Convert.ChangeType(Source, Type);
            return true;
        }

        private static char FromLiteral(char EscapeChar)
        {
            switch (EscapeChar)
            {
                case '0': return '\0';
                case 'a': return '\a';
                case 'b': return '\b';
                case 'f': return '\f';
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                case 'v': return '\v';
                default:
                    return EscapeChar;
            }
        }

        /// <summary>
        /// Read a string enclosed in quotes
        /// </summary>
        /// <param name="Stream">The stream to read from</param>
        /// <returns>The string (without quotes)</returns>
        private static string ReadString(StreamReader Stream)
        {
            var builder = new StringBuilder();
            var end = Stream.Read();
            while (!Stream.EndOfStream && Stream.Peek() != end)
            {
                var ch = Stream.Read();
                if (ch == '\\' && !Stream.EndOfStream)
                    builder.Append(FromLiteral((char)Stream.Read()));
                else
                    builder.Append((char)ch);
            }
            Stream.Read();
            return builder.ToString();
        }

        private static string ReadWord(StreamReader Stream)
        {
            var builder = new StringBuilder();
            char pk;
            while (!Stream.EndOfStream && !Char.IsSeparator(pk = (char)Stream.Peek()) && !IsTerminator(pk))
                builder.Append((char)Stream.Read());
            return builder.ToString();
        }

        private static string ReadUntil(StreamReader Stream, Func<char, bool> TestFn)
        {
            var builder = new StringBuilder();
            while (!Stream.EndOfStream && !TestFn((char)Stream.Peek()))
                builder.Append((char)Stream.Read());
            return builder.ToString();
        }

        private static void SkipWhitespace(StreamReader Stream)
        {
            while (Char.IsWhiteSpace((char)Stream.Peek()))
                Stream.Read();
        }

        private static bool IsTerminator(Char Char)
        {
            switch (Char)
            {
                case ':':
                case ';':
                case '[':
                case ']':
                case '{':
                case '}':
                    return true;
                default:
                    return false;
            }
        }
    }
}
