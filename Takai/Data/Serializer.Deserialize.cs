using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

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

        public static List<object> TextDeserializeAll(string file)
        {
            using (var reader = new StreamReader(file))
                return TextDeserializeAll(reader);
        }

        //custom suffixes

        /// <summary>
        /// Read all objects from the file
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <returns>The objects created</returns>
        public static List<object> TextDeserializeAll(StreamReader reader)
        {
            var objects = new List<object>();
            while (!reader.EndOfStream)
                objects.Add(TextDeserialize(reader));

            return objects;
        }

        public static T TextDeserialize<T>(string file, bool strict = true)
        {
            using (var reader = new StreamReader(file))
                return Cast<T>(TextDeserialize(reader), strict);
        }

        public static object TextDeserialize(string file)
        {
            using (var reader = new StreamReader(file))
                return TextDeserialize(reader);
        }

        public static T TextDeserialize<T>(StreamReader reader, bool strict = true)
        {
            return Cast<T>(TextDeserialize(reader), strict);
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
                    dict[String.Intern(key.ToString().TrimEnd())] = value; //todo: analyze interning

                    SkipIgnored(reader);

                    if ((peek = reader.Peek()) == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", reader));

                    if (peek == ';')
                    {
                        reader.Read(); //skip ;
                        SkipIgnored(reader);
                    }
                    //else if (peek != '}')
                    //    throw new InvalidDataException(ExceptString("Unexpected token. Expected ';' or '}' while trying to read object", reader));
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
                //var reference = TextDeserialize(file);
                return Cache.Load(file);
            }

            if (peek == '"' || peek == '\'')
                return ReadString(reader);

            string word;
            do
            {
                if (peek == '-' || peek == '+' || peek == '.')
                    word = (char)reader.Read() + ReadWord(reader);
                else
                    word = ReadWord(reader);
            } while (!reader.EndOfStream && word.Length < 1);

            if ("-+.".Contains(word[0]) || char.IsDigit(word[0])) //maybe handle ,
            {
                if (reader.Peek() == '.')
                    word += (char)reader.Read() + ReadWord(reader);

                string unit = string.Empty;

                //exponential form
                if (word.EndsWith("E"))
                {
                    if (reader.Peek() == '-')
                        word += (char)reader.Read();
                    word += ReadWord(reader);
                }
                else
                {
                    for (int i = word.Length - 1; i >= 0; --i)
                    {
                        if ("-+.".Contains(word[i]) || Char.IsDigit(word[i]))
                        {
                            unit = word.Substring(i + 1);
                            word = word.Substring(0, i + 1);
                            break;
                        }
                    }
                    unit = unit.TrimEnd();
                }
                
                if (TInt.TryParse(word, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var @int))
                {
                    if (unit.Length > 0)
                    {
                        if (unit.Equals("sec", StringComparison.OrdinalIgnoreCase)) //convert from seconds to milliseconds
                            return @int * 1000;
                        if (unit.Equals("min", StringComparison.OrdinalIgnoreCase)) //convert from minutes to milliseconds
                            return @int * 1000 * 60;

                        //all others get converted to float
                    }
                    else
                        return @int;
                }
                if (TFloat.TryParse(word, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var @float))
                {
                    if (unit.Length > 0)
                    {
                        if (unit.Equals("deg", StringComparison.OrdinalIgnoreCase)) //convert from degrees to radians
                            return @float / 180 * Math.PI;
                        else if (unit.Equals("sec", StringComparison.OrdinalIgnoreCase)) //convert from seconds to milliseconds
                            return @float * 1000;
                        else if (unit.Equals("min", StringComparison.OrdinalIgnoreCase)) //convert from minutes to milliseconds
                            return @float * 1000 * 60;

                        else if (!unit.Equals("rad", StringComparison.OrdinalIgnoreCase) &&
                                 !unit.Equals("msec", StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentOutOfRangeException($"{unit} is an unknown numeric suffix");
                    }
                    return @float;
                }
            }

            if (word.ToLower() == "null")
                return null;

            if (bool.TryParse(word, out var @bool))
                return @bool;

            if (RegisteredTypes.TryGetValue(word, out var type))
            {
                SkipIgnored(reader);

                peek = reader.Peek();

                if (type.IsEnum)
                {
                    peek = reader.Read();
                    if (peek == '[')
                    {
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
                    if (peek == '.')
                        return Enum.Parse(type, ReadWord(reader));

                    if (reader.Read() != '[')
                        throw new InvalidDataException(ExceptString($"Expected a '[' when reading enum value (EnumType[Key1; Key2]) or '.' (EnumType.Key) while attempting to read '{word}'", reader));
                }

                //static/single enum value
                if (peek == '.')
                {
                    reader.Read();
                    SkipIgnored(reader);
                    var staticVal = ReadWord(reader);

                    var field = type.GetField(staticVal, BindingFlags.Public | BindingFlags.Static);
                    if (field != null)
                        return field.GetValue(null);

                    var prop = type.GetProperty(staticVal, BindingFlags.Public | BindingFlags.Static);
                    if (prop != null)
                        return prop.GetValue(null);

                    throw new InvalidDataException(ExceptString($"Expected static value when reading {staticVal} from type '{word}'", reader));
                }

                //read object
                if (peek != '{')
                    throw new InvalidDataException(ExceptString($"Expected an object definition ('{{') when reading type '{word}' (Type {{ }})", reader));

                var dict = TextDeserialize(reader) as Dictionary<string, object>;
                if (dict == null)
                    throw new InvalidDataException(ExceptString($"Expected an object definition when reading type '{word}' (Type {{ }})", reader));

                try
                {
                    var obj = ParseDictionary(type, dict);
                    return obj;
                }
                catch (Exception expt)
                {
                    expt.Data.Add("Offset", GetStreamOffset(reader));
                    throw expt;
                }
            }

            throw new NotSupportedException(ExceptString($"Unknown identifier: '{word}'", reader));
        }

        public static object ParseDictionary(Type destType, Dictionary<string, object> dict)
        {
            var deserial = destType.GetCustomAttribute<CustomDeserializeAttribute>()?.deserialize;
            if (deserial != null)
            {
                var deserialied = deserial.Invoke(null, new[] { dict }); //must be static here
                if (deserialied != DefaultAction)
                    return deserialied;
            }

            var obj = Activator.CreateInstance(destType); //todo: use lambda to create
            //https://vagifabilov.wordpress.com/2010/04/02/dont-use-activator-createinstance-or-constructorinfo-invoke-use-compiled-lambda-expressions/

            foreach (var pair in dict)
            {
                try
                {
                    var field = destType.GetField(pair.Key, DefaultBindingFlags);
                    if (field != null)
                    {
                        if (!Attribute.IsDefined(field, typeof(IgnoredAttribute)))
                        {
                            var customDeserial = field.GetCustomAttribute<CustomDeserializeAttribute>(true)?.deserialize;
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

                            var cast = pair.Value == null ? null : Cast(field.FieldType, pair.Value);
                            field.SetValue(obj, cast);
                        }
                    }
                    else
                    {
                        var prop = destType.GetProperty(pair.Key, DefaultBindingFlags);
                        if (prop != null)
                        {
                            if (!Attribute.IsDefined(prop, typeof(IgnoredAttribute)))
                            {
                                var customDeserial = prop.GetCustomAttribute<CustomDeserializeAttribute>(true)?.deserialize;
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
                                else if (prop.CanWrite)
                                {
                                    var cast = pair.Value == null ? null : Cast(prop.PropertyType, pair.Value);
                                    prop.SetValue(obj, cast);
                                }
                            }
                        }
                        else
                            System.Diagnostics.Debug.WriteLine($"Ignoring unknown field:{pair.Key} in DestType:{destType.Name}");
                    }
                }
                catch (InvalidCastException expt)
                {
                    throw new InvalidCastException($"Error casting to field:{pair.Key} in DestType:{destType.Name}: {expt.Message}", expt);
                }
                catch (Exception expt)
                {
                    throw new Exception($"Error parsing field:{pair.Key} in DestType:{destType.Name}: {expt.Message}", expt);
                }
            }

            var derived = destType.GetCustomAttribute<DerivedTypeDeserializeAttribute>(true);
            derived?.deserialize?.Invoke(obj, new[] { dict });

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

        public static T Cast<T>(object Source, bool Strict = true)
        {
            return (T)Cast(typeof(T), Source, Strict);
        }

        /// <summary>
        /// Convert <see cref="Source"/> to type <see cref="DestType"/>
        /// Utilizes CustomDeserializer
        /// </summary>
        /// <param name="DestType">The type to cast to</param>
        /// <param name="Source">The source object</param>
        /// <param name="Strict">Should only cast between equivelent types (If true, casting int to bool would fail)</param>
        /// <returns>The correctly casted object</returns>
        public static object Cast(Type DestType, object Source, bool Strict = true)
        {
            if (Source == null)
                return null;

            var sourceType = Source.GetType();

            if (DestType.IsAssignableFrom(sourceType))
                return Source;

            if (Serializers.TryGetValue(DestType, out var deserial) && deserial.Deserialize != null)
                return deserial.Deserialize(Source);

            var customDeserial = DestType.GetCustomAttribute<CustomDeserializeAttribute>(false)?.deserialize;
            if (customDeserial != null)
            {
                var deserialed = customDeserial.Invoke(null, new[] { Source }); //must be static here
                if (deserialed != DefaultAction)
                    return deserialed;
            }

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
                list = list.ConvertAll(i => Cast(elType, i, Strict));
                var casted = CastMethod.MakeGenericMethod(elType).Invoke(null, new[] { list });
                return ToArrayMethod.MakeGenericMethod(elType).Invoke(null, new[] { casted });
            }

            var sourceList = Source as List<object>;
            if (DestType.IsGenericType)
            {
                var genericType = DestType.GetGenericTypeDefinition();
                var genericArgs = DestType.GetGenericArguments();

                //todo: support any number of items in tuple
                if (genericType == typeof(Tuple<,>))
                {
                    if (sourceList == null)
                        throw new InvalidCastException($"Type:{DestType.Name} is a Tuple but '{Source}' is of type:{sourceType.Name}");

                    for (int i = 0; i < sourceList.Count; ++i)
                        sourceList[i] = Cast(genericArgs[i], sourceList[i], Strict);
                    return Activator.CreateInstance(DestType, sourceList.ToArray());
                }

                //if (typeof(IEnumerable<>).IsAssignableFrom(genericType) && genericArgs.Count() == 1)
                if (genericType == typeof(List<>) || genericType == typeof(HashSet<>) ||
                    genericType == typeof(Queue<>) || genericType == typeof(Stack<>))
                {
                    if (sourceList == null)
                        throw new InvalidCastException($"Type:{DestType.Name} is a {genericType.Name} but '{Source}' is of type:{sourceType.Name}");

                    sourceList = sourceList.ConvertAll(i => Cast(genericArgs[0], i, Strict));
                    //return Activator.CreateInstance(DestType, new[] { list.AsEnumerable<object>() });
                    var casted = CastMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { sourceList });
                    return ToListMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { casted });
                }

                if (genericType == typeof(Dictionary<,>))
                {
                    //todo: revisit, may be able to convert srcDict more easily

                    var srcDict = Source as Dictionary<string, object>;
                    if (srcDict == null)
                        throw new InvalidCastException($"Type:{DestType.Name} is a dictionary but '{Source}' is of type:{sourceType.Name}");

                    var dict = Activator.CreateInstance(DestType, srcDict.Count);
                    var add = DestType.GetMethod("Add");

                    foreach (var pair in srcDict)
                        add.Invoke(dict, new[] { Cast(genericArgs[0], pair.Key, false), Cast(genericArgs[1], pair.Value, Strict) });

                    return dict;
                }
            }

            //struct initialization using shorthand array syntax
            if (DestType.IsValueType && sourceList != null)
            {
                var obj = Activator.CreateInstance(DestType);
                var members = DestType.GetMembers(BindingFlags.Instance | BindingFlags.Public);
                var memberEnumerator = members.GetEnumerator();
                for (int i = 0; i < sourceList.Count; ++i)
                {
                    if (!memberEnumerator.MoveNext())
                        throw new ArgumentOutOfRangeException($"too many members when converting to {DestType.Name}");
                    //parse dictionary (test if field or property and set accordingly)
                    switch (((MemberInfo)memberEnumerator.Current).MemberType)
                    {
                        case MemberTypes.Field:
                            var field = (FieldInfo)memberEnumerator.Current;
                            field.SetValue(obj, Cast(field.FieldType, sourceList[i], Strict));
                            break;
                        case MemberTypes.Property:
                            var prop = (PropertyInfo)memberEnumerator.Current;
                            prop.SetValue(obj, Cast(prop.PropertyType, sourceList[i], Strict));
                            break;
                        default:
                            --i; //does not seem to be built in way to filter out non-var members
                            break;
                    }
                }
                return obj;
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
            return string.Intern(builder.ToString());
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
