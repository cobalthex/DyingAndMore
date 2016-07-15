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
    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class NonSerializedAttribute : System.Attribute { }

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

    /// <summary>
    /// Allows for custom serializers of specific types
    /// </summary>
    /// <remarks>Must serialize to a primative,enum,string,array,dict,object</remarks>
    public struct CustomTypeSerializer
    {
        /// <summary>
        /// A custom serializer. Takes in the source object and outputs a known format (primative, enum, string, array, dict, object)
        /// </summary>
        public Func<object, object> Serialize;
        /// <summary>
        /// Takes in a known format and outputs the destination object
        /// </summary>
        public Func<object, object> Deserialize;
    }

    /// <summary>
    /// Serialize objects
    /// </summary>
    public static class Serializer
    {
        public const bool WriteFullTypeNames = false;

        //cached types from assemblies
        private static Dictionary<string, Type> asmTypes;

        /// <summary>
        /// Custom serializers (provided for things like system classes. User defined classes can use CustomSerializeAttribute
        /// </summary>
        public static Dictionary<Type, CustomTypeSerializer> Serializers { get; set; }
        private static readonly MethodInfo CastMethod = typeof(System.Linq.Enumerable).GetMethod("Cast");
        private static readonly MethodInfo ToListMethod = typeof(System.Linq.Enumerable).GetMethod("ToList");

        static Serializer()
        {
            asmTypes = new Dictionary<string, Type>();
            Serializers = new Dictionary<Type, CustomTypeSerializer>();
            ReloadTypes();

            //default custom serializers
            Serializers.Add(typeof(Vector2), new CustomTypeSerializer
            {
                Serialize = (object Value) => { var v = (Vector2)Value; return new[] { v.X, v.Y }; },
                Deserialize = (object Value) =>
                {
                    var v = (List<object>)Value;
                    var x = (float)Convert.ChangeType(v[0], typeof(float));
                    var y = (float)Convert.ChangeType(v[1], typeof(float));
                    return new Vector2(x, y);
                }
            });
            Serializers.Add(typeof(Point), new CustomTypeSerializer
            {
                Serialize = (object Value) => { var v = (Point)Value; return new[] { v.X, v.Y }; },
                Deserialize = (object Value) =>
                {
                    var v = (List<object>)Value;
                    var x = (int)Convert.ChangeType(v[0], typeof(int));
                    var y = (int)Convert.ChangeType(v[1], typeof(int));
                    return new Point(x, y);
                }
            });
            Serializers.Add(typeof(Rectangle), new CustomTypeSerializer
            {
                Serialize = (object Value) => { var v = (Rectangle)Value; return new[] { v.X, v.Y, v.Width, v.Height }; },
                Deserialize = (object Value) =>
                {
                    var v = (List<object>)Value;
                    var x = (int)Convert.ChangeType(v[0], typeof(int));
                    var y = (int)Convert.ChangeType(v[1], typeof(int));
                    var width = (int)Convert.ChangeType(v[2], typeof(int));
                    var height = (int)Convert.ChangeType(v[3], typeof(int));
                    return new Rectangle(x, y, width, height);
                }
            });
            Serializers.Add(typeof(Color), new CustomTypeSerializer
            {
                Serialize = (object Value) => { var v = (Color)Value; return new[] { v.R, v.G, v.B, v.A }; },
                Deserialize = (object Value) =>
                {
                    var v = (List<object>)Value;
                    var r = (int)Convert.ChangeType(v[0], typeof(int));
                    var g = (int)Convert.ChangeType(v[1], typeof(int));
                    var b = (int)Convert.ChangeType(v[2], typeof(int));
                    var a = (int)Convert.ChangeType(v[3], typeof(int));
                    return new Color(r, g, b, a);
                }
            });
            Serializers.Add(typeof(Texture2D), new CustomTypeSerializer
            {
                Serialize = (object Value) => { return ((Texture2D)Value).Name; },
                Deserialize = (object Value) => { return Takai.AssetManager.Load<Texture2D>((string)Value); }
            });
            Serializers.Add(typeof(Graphics.Graphic), new CustomTypeSerializer
            {
                Deserialize = (object Value) =>
                {
                    var str = Value as string;
                    if (str != null)
                        return new Graphics.Graphic(Takai.AssetManager.Load<Texture2D>(str));
                    return null;
                }
            });
            Serializers.Add(typeof(TimeSpan), new CustomTypeSerializer
            {
                Serialize = (object Value) => { return ((TimeSpan)Value).TotalMilliseconds; },
                Deserialize = (object Value) => { return TimeSpan.FromMilliseconds((double)Convert.ChangeType(Value, typeof(double))); }
            });
        }

        /// <summary>
        /// Reload all cached types. Should be called if/when new assemblies are added
        /// Automatically called at the beginning of the application
        /// </summary>
        public static void ReloadTypes()
        {
            var assemblies = new[] { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly() };
            foreach (Assembly ass in assemblies)
            {
                Type[] types = ass.GetTypes();
                foreach (var type in types)
                    asmTypes[WriteFullTypeNames ? type.FullName : type.Name] = type;
            }
        }

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

            if (ty.IsPrimitive)
                Stream.Write(Object);

            else if (ty.IsEnum)
                Stream.Write("{0} {1}", WriteFullTypeNames ? ty.FullName : ty.Name, Object.ToString());

            else if (ty == typeof(String) || ty == typeof(char[]))
                Stream.Write(Object.ToString().ToLiteral());

            //custom serializer
            else if (Serializers.ContainsKey(ty) && Serializers[ty].Serialize != null)
                TextSerialize(Stream, Serializers[ty].Serialize(Object), IndentLevel);

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

            //todo: dict

            else
            {
                Stream.WriteLine("{0} {{", WriteFullTypeNames ? ty.FullName : ty.Name);

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<Data.NonSerializedAttribute>() != null)
                        continue;

                    Indent(Stream, IndentLevel + 1);
                    Stream.Write("{0}: ", field.Name);

                    //user-defined serializer
                    var attr = field.GetCustomAttribute<CustomSerializeAttribute>(false)?.Serialize;
                    if (attr != null)
                        TextSerialize(Stream, attr.Invoke(null, new[] { Object }), IndentLevel + 1);
                    //normal serializer
                    else
                        TextSerialize(Stream, field.GetValue(Object), IndentLevel + 1);

                    Stream.WriteLine(";");
                }

                foreach (var field in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<Data.NonSerializedAttribute>() != null)
                        continue;

                    Indent(Stream, IndentLevel + 1);
                    Stream.Write("{0}: ", field.Name);

                    //user-defined serializer
                    var attr = field.GetCustomAttribute<CustomSerializeAttribute>()?.Serialize;
                    if (attr != null)
                        TextSerialize(Stream, attr.Invoke(null, new[] { field.GetValue(Object) }), IndentLevel + 1);
                    //normal serializer
                    else
                        TextSerialize(Stream, field.GetValue(Object), IndentLevel + 1);

                    Stream.WriteLine(";");
                }

                Stream.Write(new string(' ', IndentLevel * 4));
                Stream.Write("}");
            }
        }

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
        ///     Type {...} - Type
        ///     {...} - Dictionary&lt;string, object&gt;
        /// </remarks>
        public static object TextDeserialize(StreamReader Stream)
        {
            SkipWhitespace(Stream);

            var pk = (char)Stream.Peek();
            //strings
            if (pk == '"' || pk == '\'')
                return ReadString(Stream);
            //numbers
            if (Char.IsDigit(pk) || pk == '-' || pk == '.' || pk == ',')
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

                var d = new Dictionary<string, object>();
                while (!Stream.EndOfStream && Stream.Peek() != '}')
                {
                    var name = ReadWord(Stream);
                    if (Stream.Read() != ':')
                        throw new FormatException("Format is name:value;");

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
                        throw new FormatException(String.Format("Missing semicolon for property {0}", name));
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

            var ty = asmTypes[word];

            if (ty == null)
                return null;

            //enum
            if (ty.IsEnum)
            {
                var value = ReadWord(Stream);
                return Enum.Parse(ty, value);
            }

            var dict = TextDeserialize(Stream) as Dictionary<string, object>;

            if (dict != null)
            {
                var inst = Activator.CreateInstance(ty);

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<Data.NonSerializedAttribute>() != null)
                        continue;

                    var fty = field.FieldType;

                    if (Serializers.ContainsKey(fty) && Serializers[fty].Deserialize != null)
                        field.SetValue(inst, Serializers[fty].Deserialize(dict[field.Name]));

                    else if (dict.ContainsKey(field.Name))
                    {
                        if (dict[field.Name] is IEnumerable)
                        {
                            var ety = fty.HasElementType ? fty.GetElementType() : fty.GetGenericArguments()[0];
                            var orig = (List<object>)dict[field.Name];
                            
                            var castItems = CastMethod.MakeGenericMethod(ety).Invoke(null, new[] { orig });
                            var list = ToListMethod.MakeGenericMethod(ety).Invoke(null, new[] { castItems });

                            field.SetValue(inst, list);
                        }
                        else
                            field.SetValue(inst, dict[field.Name]);
                    }
                }

                foreach (var field in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<Data.NonSerializedAttribute>() != null)
                        continue;

                    var fty = field.PropertyType;

                    if (Serializers.ContainsKey(fty) && Serializers[fty].Deserialize != null)
                        field.SetValue(inst, Serializers[fty].Deserialize(dict[field.Name]));

                    else if (dict.ContainsKey(field.Name))
                    {
                        if (dict[field.Name] is IList)
                        {
                            var ety = fty.HasElementType ? fty.GetElementType() : fty.GetGenericArguments()[0];
                            var orig = (List<object>)dict[field.Name];

                            var castItems = CastMethod.MakeGenericMethod(ety).Invoke(null, new[] { orig });
                            var list = ToListMethod.MakeGenericMethod(ety).Invoke(null, new[] { castItems });

                            field.SetValue(inst, list);
                        }
                        else
                            field.SetValue(inst, dict[field.Name]);
                    }
                }

                return inst;
            }

            return null;
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
