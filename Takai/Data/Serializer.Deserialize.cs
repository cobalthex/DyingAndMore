using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using TFloat = System.Double;
using TInt = System.Int64;

namespace Takai.Data
{
    /// <summary>
    /// This member/object should be serizlied with the specified method
    /// If the method is an instance method, called in the context of the parent object (when available)
    ///
    /// If the method is a static method and returns Serializer.DefaultAction, the object will be deserialized using the standard object constructor
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomDeserializeAttribute : Attribute
    {
        internal MethodInfo deserialize;

        public CustomDeserializeAttribute(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            deserialize = method;
        }
    }

    /// <summary>
    /// The object should use the specified method to read custom dictionary fields from a deserialized object
    /// The reverse of DerivedTypeSerialize
    /// </summary>
    /// <remarks>Only used if a typed object is serialized (dictionaries/primatives ignored)</remarks>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DerivedTypeDeserializeAttribute : Attribute
    {
        internal MethodInfo deserialize;

        /// <summary>
        /// Create a derived type serializer
        /// </summary>
        /// <param name="type">The object to look in for the serialize method</param>
        /// <param name="methodName">The name of the method to search for (Must be non-static, can be public or non public)</param>
        /// <remarks>format: Dictionary&lt;string, object&gt;(object Source)</remarks>
        public DerivedTypeDeserializeAttribute(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            deserialize = method;
        }
    }

    public static partial class Serializer
    {
        private static readonly MethodInfo CastMethod = typeof(Enumerable).GetMethod("Cast");
        private static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethod("ToList");
        private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

        /// <summary>
        /// Specifies that a custom serializer should take the default action
        /// </summary>
        public static readonly object DefaultAction = new object();

        public static long GetStreamOffset(StreamReader reader)
        {
            int charPos = (int)reader.GetType().InvokeMember("charPos",
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField,
                            null, reader, null);

            int charLen = (int)reader.GetType().InvokeMember("charLen",
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField,
                            null, reader, null);

            return reader.BaseStream.Position - charLen + charPos;
        }

        private static string ExceptString(string BaseExceptionMessage, StreamReader Stream)
        {
            try { return BaseExceptionMessage + $" [Offset:{GetStreamOffset(Stream)}]"; }
            catch { return BaseExceptionMessage; }
        }

        public static object TextDeserialize(string file)
        {
            using (var reader = new StreamReader(file))
                return TextDeserialize(reader);
        }

        /// <summary>
        /// Deserialize a stream into an object (only reads the first object)
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <returns>The object created</returns>
        public static object TextDeserialize(StreamReader reader)
        {
            SkipIgnored(reader);

            if (reader.EndOfStream)
                return null;

            var peek = reader.Peek();

            //read dictionary
            if (peek == '{')
            {
                reader.Read(); //skip {
                SkipIgnored(reader);

                var dict = new Dictionary<string, object>(CaseSensitiveMembers ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

                while ((peek = reader.Peek()) != -1 && peek != '}')
                {
                    var key = new StringBuilder();
                    while ((peek = reader.Peek()) != ':')
                    {
                        if (peek == -1)
                            throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", reader));

                        if (peek == ';' && key.Length == 0)
                        {
                            reader.Read();
                            break;
                        }

                        key.Append((char)reader.Read());
                    }

                    if (reader.Read() == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read object", reader));

                    var value = TextDeserialize(reader);
                    dict[key.ToString().TrimEnd()] = value;

                    SkipIgnored(reader);

                    if ((peek = reader.Peek()) == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", reader));

                    if (peek == ';')
                    {
                        reader.Read(); //skip ;
                        SkipIgnored(reader);
                    }
                    else if (peek != '}')
                        throw new InvalidDataException(ExceptString("Unexpected token. Expected ';' or '}' while trying to read object", reader));
                }

                reader.Read();
                return dict;
            }

            //read list
            if (peek == '[')
            {
                reader.Read(); //skip [
                SkipIgnored(reader);

                var values = new List<object>();
                while (!reader.EndOfStream && reader.Peek() != ']')
                {
                    if ((peek = reader.Peek()) == ';')
                    {
                        reader.Read(); //skip ;
                        SkipIgnored(reader);

                        if (reader.Peek() == ';')
                            throw new InvalidDataException(ExceptString("Unexpected ';' in list (missing value)", reader));

                        continue;
                    }

                    if (peek == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read list", reader));

                    values.Add(TextDeserialize(reader));
                    SkipIgnored(reader);
                }

                if (reader.Read() == -1) //skip ]
                    throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected ']' to close list", reader));

                return values;
            }

            if (peek == '@')
            {
                reader.Read();
                var file = ReadString(reader);
                var reference = TextDeserialize(file); //todo: make generic based on file type
                return reference;
            }

            if (peek == '"' || peek == '\'')
                return ReadString(reader);

            string word;

            if (peek == '-' || peek == '+' || peek == '.')
                word = (char)reader.Read() + ReadWord(reader);
            else
                word = ReadWord(reader);

            if (word.Length > 0 && ("-+.".Contains(word[0]) || char.IsDigit(word[0])) && reader.Peek() == '.') //maybe handle ,
                word += (char)reader.Read() + ReadWord(reader);

            if (word.ToLower() == "null")
                return null;

            if (bool.TryParse(word, out var @bool))
                return @bool;

            if (TInt.TryParse(word, out var @int))
                return @int;

            if (TFloat.TryParse(word, out var @float))
                return @float;

            if (RegisteredTypes.TryGetValue(word, out var type))
            {
                SkipIgnored(reader);

                if (type.IsEnum)
                {
                    if (reader.Read() != '[')
                        throw new InvalidDataException(ExceptString($"Expected a '[' when reading enum value EnumType[Key1; Key2]) while attempting to read '{word}'", reader));

                    SkipIgnored(reader);

                    var valuesCount = 0;
                    UInt64 values = 0;

                    while (!reader.EndOfStream && reader.Peek() != ']')
                    {
                        if ((peek = reader.Peek()) == ';')
                        {
                            reader.Read(); //skip ;
                            SkipIgnored(reader);

                            if (reader.Peek() == ';')
                                throw new InvalidDataException(ExceptString("Unexpected ';' in enum (missing value)", reader));

                            continue;
                        }

                        if (peek == -1)
                            throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read enum", reader));

                        var value = ReadWord(reader);
                        values |= Convert.ToUInt64(Enum.Parse(type, value));
                        ++valuesCount;

                        SkipIgnored(reader);
                    }

                    if (reader.Read() == -1) //skip ]
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected ']' to close enum", reader));

                    if (valuesCount == 0)
                        throw new ArgumentOutOfRangeException(ExceptString("Expected at least one enum value", reader));

                    if (valuesCount > 1 && !Attribute.IsDefined(type, typeof(FlagsAttribute), true))
                        throw new ArgumentOutOfRangeException(ExceptString($"{valuesCount} enum values were given, but type:{type.Name} is not a flags enum", reader));

                    return Enum.ToObject(type, values);
                }

                //read object
                if (reader.Peek() != '{')
                    throw new InvalidDataException(ExceptString($"Expected an object definition ('{{') when reading type '{word}' (Type {{ }})", reader));

                var dict = TextDeserialize(reader) as Dictionary<string, object>;
                if (dict == null)
                    throw new InvalidDataException(ExceptString($"Expected an object definition when reading type '{word}' (Type {{ }})", reader));

                try
                {
                    return ParseDictionary(type, dict);
                }
                catch (Exception expt)
                {
                    expt.Data.Add("Offset", GetStreamOffset(reader));
                    throw expt;
                }
            }

            throw new NotSupportedException(ExceptString($"Unknown identifier: '{word}'", reader));
        }

        public static object ParseDictionary(Type DestType, Dictionary<string, object> Dict)
        {
            var deserial = DestType.GetCustomAttribute<CustomDeserializeAttribute>()?.deserialize;
            if (deserial != null)
            {
                var deserialied = deserial.Invoke(null, new[] { Dict }); //must be static here
                if (deserialied != DefaultAction)
                    return deserialied;
            }

            var obj = Activator.CreateInstance(DestType); //todo: use lambda to create
            //https://vagifabilov.wordpress.com/2010/04/02/dont-use-activator-createinstance-or-constructorinfo-invoke-use-compiled-lambda-expressions/

            foreach (var pair in Dict)
            {
                try
                {
                    var field = DestType.GetField(pair.Key, DefaultBindingFlags);
                    if (field != null)
                    {
                        if (!Attribute.IsDefined(field, typeof(IgnoredAttribute)))
                        {
                            var customDeserial = field.GetCustomAttribute<CustomDeserializeAttribute>(false)?.deserialize;
                            if (customDeserial != null)
                            {
                                if (customDeserial.IsStatic)
                                {
                                    var deserialed = customDeserial.Invoke(null, new[] { pair.Value });
                                    if (deserialed != DefaultAction)
                                        field.SetValue(obj, deserialed);
                                }
                                else
                                {
                                    customDeserial.Invoke(obj, new[] { pair.Value });
                                    return obj;
                                }
                            }

                            var cast = pair.Value == null ? null : CastType(field.FieldType, pair.Value);
                            field.SetValue(obj, cast);
                        }
                    }
                    else
                    {
                        var prop = DestType.GetProperty(pair.Key, DefaultBindingFlags);
                        if (prop != null)
                        {
                            if (!Attribute.IsDefined(prop, typeof(IgnoredAttribute)))
                            {
                                var customDeserial = prop.GetCustomAttribute<CustomDeserializeAttribute>(false)?.deserialize;
                                if (customDeserial != null)
                                {
                                    if (customDeserial.IsStatic)
                                    {
                                        var deserialed = customDeserial.Invoke(null, new[] { pair.Value });
                                        if (deserialed != DefaultAction)
                                            prop.SetValue(obj, deserialed);
                                    }
                                    else
                                    {
                                        customDeserial.Invoke(obj, new[] { pair.Value });
                                        continue;
                                    }
                                }

                                var cast = pair.Value == null ? null : CastType(prop.PropertyType, pair.Value);
                                prop.SetValue(obj, cast);
                            }
                        }
                        else
                            System.Diagnostics.Debug.WriteLine($"Ignoring unknown field:{pair.Key} in DestType:{DestType.Name}");
                    }
                }
                catch (InvalidCastException expt)
                {
                    throw new InvalidCastException($"Error casting to field:{pair.Key} in DestType:{DestType.Name}: {expt.Message}", expt);
                }
                catch (Exception expt)
                {
                    throw new Exception($"Error parsing field:{pair.Key} in DestType:{DestType.Name}: {expt.Message}", expt);
                }
            }

            var derived = DestType.GetCustomAttribute<DerivedTypeDeserializeAttribute>();
            if (derived != null)
                derived.deserialize.Invoke(obj, new[] { Dict });

            return obj;
        }

        public static bool IsIntType(Type SourceType)
        {
            switch (Type.GetTypeCode(SourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Char:
                    return true;

                case TypeCode.Object:
                    if (SourceType.IsGenericType && SourceType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return IsIntType(Nullable.GetUnderlyingType(SourceType));
                    return false;

                default:
                    return false;
            }
        }

        public static bool IsFloatType(Type SourceType)
        {
            switch (Type.GetTypeCode(SourceType))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;

                case TypeCode.Object:
                    if (SourceType.IsGenericType && SourceType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return IsFloatType(Nullable.GetUnderlyingType(SourceType));
                    return false;

                default:
                    return false;
            }
        }

        public const BindingFlags DefaultBindingFlags = BindingFlags.IgnoreCase
                                                      | BindingFlags.Instance
                                                      | BindingFlags.Public;

        public static T CastType<T>(object Source, bool Strict = true)
        {
            return (T)CastType(typeof(T), Source, Strict);
        }

        /// <summary>
        /// Convert <see cref="Source"/> to type <see cref="DestType"/>
        /// Utilizes CustomDeserializer
        /// </summary>
        /// <param name="DestType">The type to cast to</param>
        /// <param name="Source">The source object</param>
        /// <param name="Strict">Should only cast between equivelent types (If true, casting int to bool would fail)</param>
        /// <returns>The correctly casted object</returns>
        public static object CastType(Type DestType, object Source, bool Strict = true)
        {
            if (Source == null)
                return null;

            var sourceType = Source.GetType();

            if (Serializers.TryGetValue(DestType, out var deserial) && deserial.Deserialize != null)
                return deserial.Deserialize(Source);

            var customDeserial = DestType.GetCustomAttribute<CustomDeserializeAttribute>(false)?.deserialize;
            if (customDeserial != null)
            {
                var deserialed = customDeserial.Invoke(null, new[] { Source }); //must be static here
                if (deserialed != DefaultAction)
                    return deserialed;
            }

            if (DestType.IsAssignableFrom(sourceType))
                return Source;

            if (!Strict)
            {
                if (Source is string sourceString)
                {
                    //chars can be represented as numbers (or as strings if object key)
                    if (DestType == typeof(char))
                    {
                        if (TInt.TryParse(sourceString, out var @int))
                            return (char)@int;
                    }

                    if (DestType.IsEnum)
                        return Enum.Parse(DestType, sourceString);
                }


                try { return Convert.ChangeType(Source, DestType); }
                catch { }
            }

            if (Source == null && DestType.IsPrimitive)
                throw new InvalidCastException($"Type:{DestType} is primative and cannot be null");

            if (DestType.IsArray)
            {
                var list = Source as List<object>;
                if (list == null)
                    throw new InvalidCastException($"Type:{DestType.Name} is an array but '{Source}' is of type:{sourceType.Name}");

                var elType = DestType.GetElementType();
                list = list.ConvertAll(i => CastType(elType, i, Strict));
                var casted = CastMethod.MakeGenericMethod(elType).Invoke(null, new[] { list });
                return ToArrayMethod.MakeGenericMethod(elType).Invoke(null, new[] { casted });
            }

            if (DestType.IsGenericType)
            {
                var genericType = DestType.GetGenericTypeDefinition();
                var genericArgs = DestType.GetGenericArguments();

                if (genericType == typeof(List<>))
                {
                    var list = Source as List<object>;
                    if (list == null)
                        throw new InvalidCastException($"Type:{DestType.Name} is a list but '{Source}' is of type:{sourceType.Name}");

                    list = list.ConvertAll(i => CastType(genericArgs[0], i, Strict));
                    var casted = CastMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { list });
                    return ToListMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { casted });
                }

                if (genericType == typeof(HashSet<>))
                {
                    var list = Source as List<object>;
                    if (list == null)
                        throw new InvalidCastException($"Type:{DestType.Name} is a HashSet but '{Source}' is of type:{sourceType.Name}");

                    var set = Activator.CreateInstance(DestType);
                    var add = DestType.GetMethod("Add");
                    foreach (var item in list)
                        add.Invoke(set, new[] { CastType(genericArgs[0], item, Strict) });

                    return set;
                }

                if (genericType == typeof(Dictionary<,>))
                {
                    var srcDict = Source as Dictionary<string, object>;
                    if (srcDict == null)
                        throw new InvalidCastException($"Type:{DestType.Name} is a dictionary but '{Source}' is of type:{sourceType.Name}");

                    var dict = Activator.CreateInstance(DestType);
                    var add = DestType.GetMethod("Add");

                    foreach (var pair in srcDict)
                        add.Invoke(dict, new[] { CastType(genericArgs[0], pair.Key, false), CastType(genericArgs[1], pair.Value, Strict) });

                    return dict;
                }
            }

            if (sourceType == typeof(Dictionary<string, object>))
                return ParseDictionary(DestType, (Dictionary<string, object>)Source);

            bool canConvert = false;

            bool isSourceInt = IsIntType(sourceType);
            bool isSourceFloat = IsFloatType(sourceType);

            bool isDestInt = IsIntType(sourceType);
            bool isDestFloat = IsFloatType(sourceType);

            canConvert |= (isDestInt && isSourceInt);
            canConvert |= (isDestFloat || isDestInt) && (isSourceFloat || isSourceInt);

            if (canConvert)
                return Convert.ChangeType(Source, DestType);

            throw new InvalidCastException($"Error converting '{Source}' from type:{sourceType.Name} to type:{DestType.Name}");
        }

        public static char FromLiteral(char EscapeChar)
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
        public static string ReadString(StreamReader Stream)
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

        public static string ReadWord(StreamReader Stream)
        {
            var builder = new StringBuilder();
            char pk;
            while (!Stream.EndOfStream &&
                   !Char.IsSeparator(pk = (char)Stream.Peek()) &&
                   !Char.IsPunctuation(pk))
                builder.Append((char)Stream.Read());
            return builder.ToString();
        }

        /// <summary>
        /// Assumes immediately at a comment (Post SkipWhitespace)
        /// </summary>
        /// <param name="Stream"></param>
        public static void SkipComments(StreamReader Stream)
        {
            var ch = Stream.Peek();
            if (ch == -1 || ch != '#')
                return;

            Stream.Read(); //skip #
            ch = Stream.Read();

            if (ch == -1)
                return;

            //multiline comment #* .... *#
            if (ch == '*')
            {
                //read until *#
                while ((ch = Stream.Read()) != -1 && ch != '*' &&
                       (ch = Stream.Read()) != -1 && ch != '#') ;
            }
            else
                while ((ch = Stream.Read()) != -1 && ch != '\n') ;
        }

        public static void SkipWhitespace(StreamReader Stream)
        {
            int ch;
            while ((ch = Stream.Peek()) != -1 && Char.IsWhiteSpace((char)ch))
                Stream.Read();
        }

        /// <summary>
        /// Skip comments and whitespace
        /// </summary>
        public static void SkipIgnored(StreamReader Stream)
        {
            SkipWhitespace(Stream);
            while (!Stream.EndOfStream && Stream.Peek() == '#')
            {
                SkipComments(Stream);
                SkipWhitespace(Stream);
            }
        }
    }
}
