using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using TFloat = System.Double;
using TInt = System.Int64;

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

        private static string ExceptString(string BaseExceptionMessage, StreamReader Stream)
        {
            Int32 charPos = (Int32)Stream.GetType().InvokeMember("charPos",
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField,
                            null, Stream, null);

            Int32 charLen = (Int32)Stream.GetType().InvokeMember("charLen",
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField,
                            null, Stream, null);

            var pos = (Int32)Stream.BaseStream.Position - charLen + charPos;

            try { return BaseExceptionMessage + $" [Offset:{pos}]"; }
            catch { return BaseExceptionMessage; }
        }

        /// <summary>
        /// Deserialize a stream into an object (only reads the first object)
        /// </summary>
        /// <param name="Stream">The stream to read from</param>
        /// <returns>The object created</returns>
        public static object TextDeserialize(StreamReader Stream)
        {
            SkipIgnored(Stream);

            if (Stream.EndOfStream)
                return null;

            var ch = Stream.Peek();

            //read dictionary
            if (ch == '{')
            {
                Stream.Read(); //skip {
                SkipIgnored(Stream);

                var dict = new Dictionary<string, object>();

                while ((ch = Stream.Peek()) != -1 && ch != '}')
                {
                    var key = new StringBuilder();
                    while ((ch = Stream.Peek()) != ':')
                    {
                        if (ch == -1)
                            throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", Stream));

                        if (ch == ';' && key.Length == 0)
                        {
                            Stream.Read();
                            break;
                        }

                        key.Append((char)Stream.Read());
                    }

                    if (Stream.Read() == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read object", Stream));

                    var value = TextDeserialize(Stream);
                    dict[key.ToString().TrimEnd()] = value;

                    SkipIgnored(Stream);

                    if ((ch = Stream.Peek()) == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", Stream));

                    if (ch == ';')
                    {
                        Stream.Read(); //skip ;
                        SkipIgnored(Stream);
                    }
                    else if (ch != '}')
                        throw new InvalidDataException(ExceptString("Unexpected token. Expected ';' or '}' while trying to read object", Stream));
                }

                Stream.Read();
                return dict;
            }

            //read list
            if (ch == '[')
            {
                Stream.Read(); //skip [
                SkipIgnored(Stream);

                var values = new List<object>();
                while (!Stream.EndOfStream && Stream.Peek() != ']')
                {
                    if ((ch = Stream.Peek()) == ';')
                    {
                        Stream.Read(); //skip ;
                        SkipIgnored(Stream);

                        if (Stream.Peek() == ';')
                            throw new InvalidDataException(ExceptString("Unexpected ';' in list (missing value)", Stream));

                        continue;
                    }

                    if (ch == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read list", Stream));

                    values.Add(TextDeserialize(Stream));
                    SkipIgnored(Stream);
                }

                if (Stream.Read() == -1) //skip ]
                    throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected ']' to close list", Stream));

                return values;
            }

            if (ch == '@')
                throw new NotImplementedException("File references are not currently implemented");

            if (ch == '"' || ch == '\'')
                return ReadString(Stream);

            string word;
            if (ch == '-' || ch == '+' || ch == '.')
                word = (char)Stream.Read() + ReadWord(Stream);
            else
                word = ReadWord(Stream);

            if (word.Length > 0 && ("-+.".Contains(word[0]) || char.IsDigit(word[0])) && Stream.Peek() == '.') //maybe handle ,
                word += (char)Stream.Read() + ReadWord(Stream);

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
                SkipIgnored(Stream);

                if (type.IsEnum)
                {
                    if (Stream.Read() != '[')
                        throw new InvalidDataException(ExceptString($"Expected a '[' when reading enum value EnumType[Key1; Key2]) while attempting to read '{word}'", Stream));

                    SkipIgnored(Stream);

                    var valuesCount = 0;
                    UInt64 values = 0;

                    while (!Stream.EndOfStream && Stream.Peek() != ']')
                    {
                        if ((ch = Stream.Peek()) == ';')
                        {
                            Stream.Read(); //skip ;
                            SkipIgnored(Stream);

                            if (Stream.Peek() == ';')
                                throw new InvalidDataException(ExceptString("Unexpected ';' in enum (missing value)", Stream));

                            continue;
                        }

                        if (ch == -1)
                            throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read enum", Stream));

                        var value = ReadWord(Stream);
                        values |= Convert.ToUInt64(Enum.Parse(type, value));
                        valuesCount++;

                        SkipIgnored(Stream);
                    }

                    if (Stream.Read() == -1) //skip ]
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected ']' to close enum", Stream));

                    if (valuesCount == 0)
                        throw new ArgumentOutOfRangeException(ExceptString("Expected at least one enum value", Stream));

                    if (valuesCount > 1 && !Attribute.IsDefined(type, typeof(FlagsAttribute), true))
                        throw new ArgumentOutOfRangeException(ExceptString($"{valuesCount} enum values were given, but type:{type.Name} is not a flags enum", Stream));

                    return Enum.ToObject(type, values);
                }

                //todo: factor [] and {} code into functions that take in callback for parsing args (and then have Type { } use directly)

                //read object
                if (Stream.Peek() != '{')
                    throw new InvalidDataException(ExceptString($"Expected an object definition ('{{') when reading type '{word}' (Type {{ }})", Stream));

                var dict = TextDeserialize(Stream) as Dictionary<string, object>;
                if (dict == null)
                    throw new InvalidDataException(ExceptString($"Expected an object definition when reading type '{word}' (Type {{ }})", Stream));

                var obj = Activator.CreateInstance(type);
                foreach (var pair in dict)
                {
                    //try
                    {
                        var field = type.GetField(pair.Key, DefaultBindingFlags);
                        if (field != null)
                        {
                            if (!Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                            {
                                var customDeserial = field.GetCustomAttribute<CustomDeserializeAttribute>(false)?.Deserialize;
                                if (customDeserial != null)
                                    return customDeserial.Invoke(null, new[] { pair.Value });

                                var cast = CastType(field.FieldType, pair.Value);
                                field.SetValue(obj, cast);
                            }
                        }
                        else
                        {
                            var prop = type.GetProperty(pair.Key, DefaultBindingFlags);
                            if (prop != null)
                            {
                                if (!Attribute.IsDefined(prop, typeof(NonSerializedAttribute)))
                                {
                                    var customDeserial = prop.GetCustomAttribute<CustomDeserializeAttribute>(false)?.Deserialize;
                                    if (customDeserial != null)
                                        return customDeserial.Invoke(null, new[] { pair.Value });

                                    var cast = CastType(prop.PropertyType, pair.Value);
                                    prop.SetValue(obj, cast);
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"Unknown field:{pair.Key} in type:{type.Name}. Ignoring");
                        }
                    }
                    //catch (InvalidCastException expt)
                    //{
                    //    throw new InvalidCastException(ExceptString($"Error casting to field:{pair.Key} in type:{type.Name}: {expt.Message}", Stream), expt);
                    //}
                    //catch (Exception expt)
                    //{
                    //    throw new Exception(ExceptString($"Error parsing field:{pair.Key} in type:{type.Name}: {expt.Message}", Stream), expt);
                    //}
                }
                return obj;
            }

            throw new NotSupportedException(ExceptString($"Unknown identifier: '{word}'", Stream));
        }

        public const BindingFlags DefaultBindingFlags = BindingFlags.IgnoreCase
                                                      | BindingFlags.Instance
                                                      | BindingFlags.Public;

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

        /// <summary>
        /// Convert <see cref="Source"/> to type <see cref="Ty"/>
        /// Utilizes CustomSerializers
        /// </summary>
        /// <param name="DestType">The type to cast to</param>
        /// <param name="Source">The source object</param>
        /// <param name="Strict">Should only cast between equivelent types (If true, casting int to bool would fail)</param>
        /// <returns>The correctly casted object</returns>
        public static object CastType(Type DestType, object Source, bool Strict = true)
        {
            var sourceType = Source.GetType();

            if (Serializers.TryGetValue(DestType, out var deserial) && deserial.Deserialize != null)
                return deserial.Deserialize(Source);

            var customDeserial = DestType.GetCustomAttribute<CustomDeserializeAttribute>(false)?.Deserialize;
            if (customDeserial != null)
                return customDeserial.Invoke(null, new[] { Source });

            if (DestType.IsAssignableFrom(sourceType))
                return Source;

            if (!Strict)
            {
                try { return Convert.ChangeType(Source, DestType); }
                catch { }

                var sourceString = Source as string;
                if (sourceString != null && DestType.IsEnum)
                    return Enum.Parse(DestType, sourceString);
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
