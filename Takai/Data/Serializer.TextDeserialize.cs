using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Linq.Expressions;

using TFloat = System.Double;
using TInt = System.Int64;

//todo: proper lazy load support for ext references

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
    /// Specifies which member of a class can be used to accept nested objects from the Tk syntax
    /// e.g.
    /// <code>List {
    ///     ChildItem1 { }
    ///     ChildItem2 { }
    /// }</code>.
    /// Custom derived types work as well
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class AutoChildrenAttribute : Attribute
    {
        internal string memberName;
        internal MemberInfo member;

        public AutoChildrenAttribute(string member)
        {
            memberName = member;
        }
    }

    /// <summary>
    /// Allows for custom deserialization on top of the default deserialization
    /// </summary>
    public interface IDerivedDeserialize
    {
        void DerivedDeserialize(Dictionary<string, object> props);
    }

    delegate object ObjectActivator();

    public static partial class Serializer
    {
        private static readonly MethodInfo CastMethod = typeof(Enumerable).GetMethod("Cast");
        private static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethod("ToList");
        private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

        /// <summary>
        /// Specifies that a custom serializer should take the default action
        /// </summary>
        public static readonly object DefaultAction = new object();

        public class PendingResolution
        {
            public string id;
            public object target;
            public int listIndex;
            public string dictionaryKey;
            public Action<object> objectSetter;
        }

        public class DeserializationContext
        {
            internal TextReader reader; //public?

            public string file;
            public string root;

            public Dictionary<string, object> resolverCache = new Dictionary<string, object>
                (CaseSensitiveIdentifiers ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<PendingResolution>> pendingCache = new Dictionary<string, List<PendingResolution>>
                (CaseSensitiveIdentifiers ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            public void AddPending(PendingResolution pending)
            {
                if (!pendingCache.TryGetValue(pending.id, out var prList))
                    pendingCache[pending.id] = prList = new List<PendingResolution>();
                prList.Add(pending);
            }
        }

        public static long GetStreamOffset(TextReader reader)
        {
            if (!(reader is StreamReader stream))
                return 0;

#if WINDOWS
            int charPos = (int)stream.GetType().InvokeMember("charPos",
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField,
                            null, stream, null);

            int charLen = (int)stream.GetType().InvokeMember("charLen",
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField,
                            null, stream, null);

            return stream.BaseStream.Position - charLen + charPos;
#else
            return 0; //todo
#endif
        }

        internal static string GetExceptionMessage(string error, DeserializationContext context)
        {
            return error; //todo
        }

        static void Warn(string message, DeserializationContext context)
        {
            System.Diagnostics.Debug.WriteLine(message + $" <{context.file}>");
        }

        public static T TextDeserialize<T>(string file)
        {
            var context = new DeserializationContext
            {
                file = file,
                // ↓ FileShare.Readwrite may not be wanted
                reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            };
            return TextDeserialize<T>(context);
        }

        public static T TextDeserialize<T>(DeserializationContext context)
        {
            return Cast<T>(TextDeserialize(context), context);
        }

        /// <summary>
        /// Deserialize a stream into an object (only reads the first object)
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <returns>The object created</returns>
        public static object TextDeserialize(DeserializationContext context)
        {
            SkipIgnored(context.reader);

            if (context.reader.Peek() == -1)
                return null;

            var peek = context.reader.Peek();

            //read dictionary
            if (peek == '{')
            {
                context.reader.Read(); //skip {
                SkipIgnored(context.reader);

                var dict = new Dictionary<string, object>(CaseSensitiveIdentifiers ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

                while ((peek = context.reader.Peek()) != -1 && peek != '}')
                {
                    var key = new StringBuilder();
                    while ((peek = context.reader.Peek()) != ':')
                    {
                        if (peek == -1)
                            throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream. Expected '}}' to close object", context));

                        if (peek == ';' && key.Length == 0)
                        {
                            context.reader.Read();
                            break;
                        }

                        key.Append((char)context.reader.Read());
                    }

                    if (context.reader.Read() == -1)
                        throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream while trying to read object", context));

                    var keyStr = key.ToString().TrimEnd();
                    //#if WINDOWS
                    string.Intern(keyStr); //todo: necessary?
                                           //#endif

                    var value = TextDeserialize(context);
                    if (value is PendingResolution pr)
                    {
                        pr.target = dict;
                        pr.dictionaryKey = keyStr;
                        context.AddPending(pr);
                        //value = default;
                    }

                    if (dict.ContainsKey(keyStr))
                        Warn($"Property '{keyStr}' was duplicated in {context.file}", context);

                    dict[keyStr] = value;

                    SkipIgnored(context.reader);

                    if ((peek = context.reader.Peek()) == -1)
                        throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream. Expected '}}' to close object", context));

                    if (peek == ';')
                    {
                        context.reader.Read(); //skip ;
                        SkipIgnored(context.reader);
                    }
                    //else if (peek != '}')
                    //    throw new InvalidDataException(GetExceptionMessage($"Unexpected token. Expected ';' or '}' while trying to read object", context));
                }

                context.reader.Read();
                return dict;
            }

            //read list
            if (peek == '[')
            {
                context.reader.Read(); //skip [
                SkipIgnored(context.reader);

                var values = new List<object>();
                while (context.reader.Peek() > 0 && context.reader.Peek() != ']')
                {
                    if ((peek = context.reader.Peek()) == ';')
                    {
                        context.reader.Read(); //skip ;
                        SkipIgnored(context.reader);

                        if (context.reader.Peek() == ';')
                            throw new InvalidDataException(GetExceptionMessage($"Unexpected ';' in list (missing value)", context));

                        continue;
                    }

                    if (peek == -1)
                        throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream while trying to read list", context));

                    var value = TextDeserialize(context);
                    if (value is PendingResolution pr)
                    {
                        pr.target = values;
                        pr.listIndex = values.Count;
                        context.AddPending(pr);
                        value = default;
                    }

                    values.Add(value);
                    SkipIgnored(context.reader);
                }

                if (context.reader.Read() == -1) //skip ]
                    throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream. Expected ']' to close list", context));

                return values;
            }

            //external references
            //@"Root/File"
            //@"./CwdFile"
            //@"." <- self
            //@!"ForceReload"
            //@@"Load All"
            //@!@"Force load All"
            if (peek == '@')
            {
                var force = false;
                context.reader.Read();
                if (context.reader.Peek() == '!')
                {
                    context.reader.Read();
                    force = true;
                }

                object loaded;
                if (context.reader.Peek() == '.') //recursive reference
                {
                    context.reader.Read();
                    loaded = Cache.Load(context.file, context.root, force);
                }
                else
                {
                    bool loadAllDefs = false;
                    if (context.reader.Peek() == '@')
                    {
                        loadAllDefs = true;
                        context.reader.Read();
                    }

                    var file = ReadString(context.reader);

                    if (context.file != null && (file.StartsWith("./") || file.StartsWith(".\\")))
                        file = Path.Combine(Path.GetDirectoryName(context.file), file.Substring(2));

                    loaded = Cache.Load(file, context.root, force, loadAllDefs);
                }

                if (loaded is IReferenceable ir)
                    ResolveRefernce(context, ir);

                return loaded;
            }

            //internal referecnes
            if (peek == '*')
            {
                context.reader.Read();
                var sb = new StringBuilder();
                do
                {
                    sb.Append((char)context.reader.Read());
                    sb.Append(ReadWord(context.reader));
                } while (context.reader.Peek() == '.' || context.reader.Peek() == '_');

                var refname = sb.ToString().TrimEnd();

                //reference already resolved
                if (context.resolverCache.TryGetValue(refname, out var resolved))
                    return resolved;

                return new PendingResolution { id = refname };
            }

            if (peek == '"' || peek == '\'')
                return ReadString(context.reader);

            string word;
            if (peek == '-' || peek == '+' || peek == '.')
                word = (char)context.reader.Read() + ReadWord(context.reader);
            else
                word = ReadWord(context.reader);

            if (word.Length == 0)
                throw new Exception("Invalid char: " + (char)peek); //todo: proper exception

            if ("-+.".Contains(word[0]) || char.IsDigit(word[0])) //maybe handle ,
            {
                if (context.reader.Peek() == '.')
                    word += (char)context.reader.Read() + ReadWord(context.reader);

                string unit = string.Empty;

                //exponential form
                if (word.EndsWith("E") || word.EndsWith("e"))
                {
                    if (context.reader.Peek() == '+')
                        context.reader.Read();
                    if (context.reader.Peek() == '-')
                        word += (char)context.reader.Read();
                    word += ReadWord(context.reader);
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

                if (context.reader.Peek() == '%')
                {
                    unit = "%";
                    context.reader.Read();
                }

                if (unit.Equals("infinity", StringComparison.OrdinalIgnoreCase))
                    return word.Equals("-", StringComparison.Ordinal) ? float.NegativeInfinity : float.PositiveInfinity;

                if (TInt.TryParse(word, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var @int))
                {
                    if (unit.Length > 0)
                    {
                        if (unit.Equals("sec", StringComparison.OrdinalIgnoreCase)) //convert from seconds to milliseconds
                            return @int * 1000;
                        if (unit.Equals("min", StringComparison.OrdinalIgnoreCase)) //convert from minutes to milliseconds
                            return @int * 1000 * 60;
                        else if (unit.Equals("rpm", StringComparison.OrdinalIgnoreCase)) //convert from rounds per minute (rpm) to milliseconds
                            return (60 * 1000) / @int;
                        else if (unit.Equals("fps", StringComparison.OrdinalIgnoreCase) ||
                                 unit.Equals("Hz", StringComparison.OrdinalIgnoreCase)) //convert from frames per second or Hertz to milliseconds
                            return 1000 / @int;

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
                        else if (unit.Equals("hr", StringComparison.OrdinalIgnoreCase)) //convert from hours to milliseconds
                            return @float * 1000 * 60 * 60;
                        else if (unit.Equals("days", StringComparison.OrdinalIgnoreCase)) //convert from days to milliseconds
                            return @float * 1000 * 60 * 60 * 24;
                        else if (unit.Equals("pi", StringComparison.OrdinalIgnoreCase)) //multiply value by pi
                            return @float * Math.PI;
                        else if (unit.Equals("%", StringComparison.OrdinalIgnoreCase)) //convert from percentage to decimal
                            return @float / 100;
                        else if (unit.Equals("rpm", StringComparison.OrdinalIgnoreCase)) //convert from rounds per minute (rpm) to milliseconds
                            return (60 * 1000) / @float;
                        else if (unit.Equals("fps", StringComparison.OrdinalIgnoreCase) ||
                                 unit.Equals("Hz", StringComparison.OrdinalIgnoreCase)) //convert from frames per second or Hertz to milliseconds
                            return 1000 / @float;

                        else if (!unit.Equals("rad", StringComparison.OrdinalIgnoreCase) &&
                                 !unit.Equals("msec", StringComparison.OrdinalIgnoreCase) &&
                                 !unit.Equals("px", StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentOutOfRangeException($"{unit} is an unknown numeric suffix");
                    }
                    return @float;
                }
            }

            if (word.Equals("nan", StringComparison.OrdinalIgnoreCase))
                return float.NaN;
            if (word.Equals("infinity", StringComparison.OrdinalIgnoreCase))
                return float.PositiveInfinity;

            if (word.ToLower() == "null")
                return null;

            if (bool.TryParse(word, out var @bool))
                return @bool;

            if (RegisteredTypes.TryGetValue(word, out var type))
            {
                SkipIgnored(context.reader);

                peek = context.reader.Peek();

                var typeInfo = type.GetTypeInfo();

                if (typeInfo.IsEnum)
                {
                    peek = context.reader.Read();
                    if (peek == '[')
                    {
                        SkipIgnored(context.reader);

                        var valuesCount = 0;
                        UInt64 values = 0;

                        while (context.reader.Peek() > 0 && context.reader.Peek() != ']')
                        {
                            if ((peek = context.reader.Peek()) == ';')
                            {
                                context.reader.Read(); //skip ;
                                SkipIgnored(context.reader);

                                if (context.reader.Peek() == ';')
                                    throw new InvalidDataException(GetExceptionMessage($"Unexpected ';' in enum (missing value)", context));

                                continue;
                            }

                            if (peek == -1)
                                throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream while trying to read enum", context));

                            //todo: support either ~ or ! in front of value to negate?

                            var value = ReadWord(context.reader);
                            values |= Convert.ToUInt64(Enum.Parse(type, value));
                            ++valuesCount;

                            SkipIgnored(context.reader);
                        }

                        if (context.reader.Read() == -1) //skip ]
                            throw new EndOfStreamException(GetExceptionMessage($"Unexpected end of stream. Expected ']' to close enum", context));

                        if (valuesCount == 0)
                            throw new ArgumentOutOfRangeException(GetExceptionMessage($"Expected at least one enum value", context));

                        if (valuesCount > 1 && !typeInfo.IsDefined(typeof(FlagsAttribute), true))
                            throw new ArgumentOutOfRangeException(GetExceptionMessage($"{valuesCount} enum values were given, but type:{type.Name} is not a flags enum", context));

                        return Enum.ToObject(type, values);
                    }
                    if (peek == '.')
                        return Enum.Parse(type, ReadWord(context.reader), !CaseSensitiveIdentifiers);

                    if (context.reader.Read() != '[')
                        throw new InvalidDataException(GetExceptionMessage($"Expected a '[' when reading enum value (EnumType[Key1; Key2]) or '.' (EnumType.Key) while attempting to read '{word}'", context));
                }

                //static/single enum value
                if (peek == '.')
                {
                    context.reader.Read();
                    SkipIgnored(context.reader);
                    var staticVal = ReadWord(context.reader);

                    var field = type.GetField(staticVal, BindingFlags.Public | BindingFlags.Static | (CaseSensitiveIdentifiers ? 0 : BindingFlags.IgnoreCase));
                    if (field != null)
                        return field.GetValue(null);

                    var prop = type.GetProperty(staticVal, BindingFlags.Public | BindingFlags.Static | (CaseSensitiveIdentifiers ? 0 : BindingFlags.IgnoreCase));
                    if (prop != null)
                        return prop.GetValue(null);

                    throw new InvalidDataException(GetExceptionMessage($"Expected static value when reading '{staticVal}' from type '{word}'", context));
                }

                //explicit type struct syntax
                if (peek == '[')
                {
                    var list = TextDeserialize(context) as List<object>;
                    var dest = Cast(type, list, context);

                    if (dest is IReferenceable ir)
                        ResolveRefernce(context, ir);

                    return dest;
                }

                //read object
                if (peek != '{')
                    throw new InvalidDataException(GetExceptionMessage($"Expected an object definition ('{{') when reading type '{word}' (Type {{ }})", context));

                var dict = TextDeserialize(context) as Dictionary<string, object>;
                if (dict == null)
                    throw new InvalidDataException(GetExceptionMessage($"Expected an object definition when reading type '{word}' (Type {{ }})", context));

                try
                {
                    var dest = CreateType(type);
                    ParseDictionary(type, dest, dict, context);

                    if (dest is IReferenceable ir)
                        ResolveRefernce(context, ir);

                    return dest;
                }
                catch (Exception expt)
                {
                    expt.Data.Add("Offset", GetStreamOffset(context.reader));
                    throw expt;
                }
            }

            throw new NotSupportedException(GetExceptionMessage($"Unknown identifier: '{word}'", context));
        }

        static void ResolveRefernce(DeserializationContext context, IReferenceable reference)
        {
            if (reference.Name == null)
                return;

            var refName = reference.GetType().Name + "." + reference.Name;
            context.resolverCache[refName] = reference;
            if (context.pendingCache.TryGetValue(refName, out var pending))
            {
                foreach (var entry in pending)
                {
                    if (entry.objectSetter != null)
                        entry.objectSetter.Invoke(reference);

                    else if (entry.target is System.Collections.IList list)
                        list[entry.listIndex] = reference;

                    else if (entry.target is System.Collections.IDictionary dict)
                        dict[entry.dictionaryKey] = reference;

                    else
                        throw new ArgumentException();
                }
                context.pendingCache.Remove(refName);
            }
        }

        static void ParseMember(object dest, object value, MemberInfo member, Type memberType, Action<object, object> setter, bool canWrite, DeserializationContext context)
        {
            var customDeserial = member.GetCustomAttribute<CustomDeserializeAttribute>(true)?.deserialize;
            if (customDeserial != null)
            {
                if (canWrite && customDeserial.IsStatic)
                {
                    var deserialed = customDeserial.Invoke(null, new[] { value });
                    if (deserialed != DefaultAction)
                        setter.Invoke(dest, deserialed);
                }
                else
                    customDeserial.Invoke(dest, new[] { value });
            }
            else if (canWrite)
            {
                var cast = value == null ? null : Cast(memberType, value, context);
                setter?.Invoke(dest, cast);

                if (member.IsDefined(typeof(AsReferenceAttribute)))
                {
                    //inefficient
                    foreach (var pending in context.pendingCache)
                    {
                        foreach (var pend in pending.Value)
                        {
                            if (ReferenceEquals(value, pend.target))
                                pend.target = cast;
                        }
                    }
                }
            }
        }

        static object CreateType(Type type) //requires empty constructor
        {
            var typeInfo = type.GetTypeInfo();
            object obj;
            if (typeInfo.IsValueType)
                obj = Activator.CreateInstance(type);
            else
            {
                NewExpression newExp = Expression.New(type.GetConstructor(Type.EmptyTypes));
                var lambda = Expression.Lambda(typeof(ObjectActivator), newExp);
                obj = ((ObjectActivator)lambda.Compile()).Invoke();
            }
            return obj;
        }

        public static T ParseDictionary<T>(Dictionary<string, object> dict, object destObject, DeserializationContext context = default(DeserializationContext))
        {
            return (T)ParseDictionary(typeof(T), destObject, dict, context);
        }

        public static object ParseDictionary(Type destType, object destObject, Dictionary<string, object> dict, DeserializationContext context = default(DeserializationContext))
        {
            var destTypeInfo = destType.GetTypeInfo();

            var deserial = destTypeInfo.GetCustomAttribute<CustomDeserializeAttribute>()?.deserialize;
            if (deserial != null)
            {
                var deserialied = deserial.Invoke(null, new[] { dict }); //must be static here
                if (deserialied != DefaultAction)
                    return deserialied;
            }

            var derived = destObject as IDerivedDeserialize;

            //todo: remap pending

            foreach (var pair in dict)
            {
                var late = pair.Value as Cache.LateBindLoad;
                var pending = pair.Value as PendingResolution;

                try
                {
                    var field = destType.GetField(pair.Key, DefaultBindingFlags);
                    if (field != null && !field.IsDefined(typeof(IgnoredAttribute)))
                    {
                        //todo: dynamic dest/target object in late bind load and references, yay or nay?

                        if (pending != null) //convert pending dictionary to pending object
                        {
                            pending.objectSetter = (value) => ParseMember(destObject, value, field, field.FieldType, field.SetValue, !field.IsInitOnly, context);
                            pending.target = destObject;
                            pending.dictionaryKey = null;
                        }
                        else if (late != null) //////////////////////todo: value fix here~~~~~~~~~
                            late.setter = (value) => ParseMember(destObject, value, field, field.FieldType, field.SetValue, !field.IsInitOnly, context);
                        else
                            ParseMember(destObject, pair.Value, field, field.FieldType, field.SetValue, !field.IsInitOnly, context);
                    }
                    else
                    {
                        PropertyInfo prop;
                        try
                        {
                            prop = destType.GetProperty(pair.Key, DefaultBindingFlags);
                        }
                        catch (AmbiguousMatchException)
                        {
                            //ugly hack (to correctly reference "new" properties)
                            prop = destType.GetProperty(pair.Key, DefaultBindingFlags | BindingFlags.DeclaredOnly);
                        }

                        if (prop != null && !prop.IsDefined(typeof(IgnoredAttribute)))
                        {
                            if (pending != null) //convert pending dictionary to pending object
                                pending.objectSetter = (value) => ParseMember(destObject, value, prop, prop.PropertyType, prop.SetValue, prop.CanWrite, context);
                            else if (late != null)
                                late.setter = (value) => ParseMember(destObject, value, prop, prop.PropertyType, prop.SetValue, prop.CanWrite, context);
                            else
                                ParseMember(destObject, pair.Value, prop, prop.PropertyType, prop.SetValue, prop.CanWrite, context);
                        }
                        else if (derived != null)
                            Warn($"possible unknown field:{pair.Key} in DestType:{destType.Name} (May be derived)", context);
                        else
                            Warn($"Ignoring unknown field:{pair.Key} in DestType:{destType.Name}", context);
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

            if (derived != null)
                derived.DerivedDeserialize(dict);

            return destObject;
        }

        public static bool IsInt(Type t)
        {
            switch (Type.GetTypeCode(t))
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

                default:
                    {
                        var ti = t.GetTypeInfo();
                        if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(Nullable<>))
                            return IsInt(Nullable.GetUnderlyingType(t));
                        return false;
                    }
            }
        }

        public static bool IsFloat(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                //case TypeCode.Decimal:
                    return true;

                default:
                    {
                        var ti = t.GetTypeInfo();
                        if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(Nullable<>))
                            return IsFloat(Nullable.GetUnderlyingType(t));
                        return false;
                    }
            }
        }

        public const BindingFlags DefaultBindingFlags = BindingFlags.IgnoreCase
                                                      | BindingFlags.Instance
                                                      | BindingFlags.Public;

        public static T Cast<T>(object source, DeserializationContext context = default)
        {
            return (T)Cast(typeof(T), source, context);
        }

        /// <summary>
        /// Convert <see cref="Source"/> to type <see cref="DestType"/>
        /// Utilizes CustomDeserializer
        /// </summary>
        /// <param name="destType">The type to cast to</param>
        /// <param name="source">The source object</param>
        /// <param name="isStrict">Should only cast between equivelent types (If true, casting int to bool would fail)</param>
        /// <returns>The correctly casted object</returns>
        public static object Cast(Type destType, object source, DeserializationContext context = default)
        {
            if (source == null)
                return null;

            var destTypeInfo = destType.GetTypeInfo();
            var sourceType = source.GetType();
            var sourceTypeInfo = sourceType.GetTypeInfo();

            if (destType.IsAssignableFrom(sourceType))
                return source;

            if (Serializers.TryGetValue(destType, out var deserial) && deserial.Deserialize != null)
            {
                var deserialized = deserial.Deserialize(source, context);
                if (deserialized != DefaultAction)
                    return deserialized;
            }

            var customDeserial = destTypeInfo.GetCustomAttribute<CustomDeserializeAttribute>(false)?.deserialize;
            if (customDeserial != null)
            {
                var deserialed = customDeserial.Invoke(null, new[] { source }); //must be static here
                if (deserialed != DefaultAction)
                    return deserialed;
            }

            //GetConstructor ?

            if (source is string sourceString)
            {
                //chars can be represented as numbers (or as strings if object key)
                if (destType == typeof(char))
                {
                    TInt @int;
                    if (sourceString.StartsWith("0x"))
                    {
                        //ugly garbage
                        if (TInt.TryParse(sourceString.Substring(2), NumberStyles.HexNumber, 
                            CultureInfo.InvariantCulture, out @int))
                            return (char)@int;
                    }
                    else if (TInt.TryParse(sourceString, out @int))
                        return (char)@int;
                }

                if (destTypeInfo.IsEnum)
                    return Enum.Parse(destType, sourceString);

                if (destType == typeof(Type))
                {
                    RegisteredTypes.TryGetValue(sourceString, out var outType);
                    return outType;
                }
            }

            if (source == null && destTypeInfo.IsPrimitive)
                throw new InvalidCastException($"Type:{destType} is primative and cannot be null");

            if (destTypeInfo.IsArray)
            {
                var list = source as List<object>;
                if (list == null)
                    throw new InvalidCastException($"Type:{destType.Name} is an array but '{source}' is of type:{sourceType.Name}");

                var elType = destType.GetElementType();
                list = list.Select(i => Cast(elType, i, context)).ToList(); //todo: List.ConvertAll (doesn't work on .net core)
                var casted = CastMethod.MakeGenericMethod(elType).Invoke(null, new[] { list });
                return ToArrayMethod.MakeGenericMethod(elType).Invoke(null, new[] { casted });
            }

            var sourceList = source as System.Collections.IList;
            if (destTypeInfo.IsGenericType)
            {
                var genericType = destType.GetGenericTypeDefinition();
                var genericArgs = destType.GetGenericArguments();

                //implicit cast (limited support)
                if (genericArgs.Length == 1 && !sourceTypeInfo.IsGenericType)
                {
                    var implCast = destType.GetMethod("op_Implicit", new[] { genericArgs[0] });
                    if (implCast != null)
                    {
                        var genericCvt = Cast(genericArgs[0], source, context);
                        return implCast.Invoke(null, new[] { genericCvt });
                    }
                }

                if (genericType == typeof(Lazy<>))
                {
                    //since the value has already been provided, this simply passes it through
                    var val = Cast(genericArgs[0], source, context);
                    var expr = Expression.Lambda(typeof(Func<>).MakeGenericType(genericArgs[0]), Expression.Constant(val)).Compile();
                    return Activator.CreateInstance(destType, expr);
                }

                //its hacks all the way down
                if (sourceType.IsGenericType &&
                    genericType == typeof(System.Collections.ObjectModel.ObservableCollection<>) &&
                    sourceType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return Activator.CreateInstance(destType, source);
                }

                if (sourceList != null)
                {
                    //todo: support any number of items in tuple
                    if (genericType == typeof(Tuple<,>))
                    {
                        if (sourceList == null)
                            throw new InvalidCastException($"Type:{destType.Name} is a Tuple but '{source}' is of type:{sourceType.Name}");

                        for (int i = 0; i < sourceList.Count; ++i)
                            sourceList[i] = Cast(genericArgs[i], sourceList[i], context);
                        return Activator.CreateInstance(destType, sourceList);
                    }
                    //if (typeof(IEnumerable<>).IsAssignableFrom(genericType) && genericArgs.Count() == 1)
                    if (genericType == typeof(HashSet<>) ||
                        genericType == typeof(Queue<>) ||
                        genericType == typeof(Stack<>) ||
                        genericType == typeof(System.Collections.ObjectModel.ObservableCollection<>))
                    {
                        if (sourceList == null)
                            throw new InvalidCastException($"Type:{destType.Name} is a {genericType.Name} but '{source}' is of type:{sourceType.Name}");

                        for (var i = 0; i < sourceList.Count; ++i)
                            sourceList[i] = Cast(genericArgs[0], sourceList[i], context);
                        var casted = CastMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { sourceList });
                        return Activator.CreateInstance(destType, new[] { casted });
                        //return ToListMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { casted });
                    }

                    if (genericType == typeof(List<>) ||
                        genericType == typeof(IEnumerable<>))
                    {
                        for (var i = 0; i < sourceList.Count; ++i)
                            sourceList[i] = Cast(genericArgs[0], sourceList[i], context);
                        var casted = CastMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { sourceList });
                        return ToListMethod.MakeGenericMethod(genericArgs[0]).Invoke(null, new[] { casted });
                        //return Activator.CreateInstance(destType, new[] { casted });
                    }

                    if (genericType == typeof(Dictionary<,>))
                    {
                        var kvpType = sourceType.GenericTypeArguments[0];
                        if (kvpType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                            throw new InvalidCastException($"Type:{destType.Name} is a dictionary but '{source}' is not a list of key value pairs");

                        var dict = Activator.CreateInstance(destType, sourceList.Count);
                        var add = destType.GetMethod("Add");

                        // this is gross
                        var keyGetter = kvpType.GetProperty(nameof(KeyValuePair<int,int>.Key), BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
                        var valGetter = kvpType.GetProperty(nameof(KeyValuePair<int,int>.Value), BindingFlags.Instance | BindingFlags.Public).GetGetMethod();

                        var invokeArgs = new object[2];
                        foreach (var pair in sourceList)
                        {
                            invokeArgs[0] = Cast(genericArgs[0], keyGetter.Invoke(pair, null), context);
                            invokeArgs[1] = Cast(genericArgs[1], valGetter.Invoke(pair, null), context);
                            add.Invoke(dict, invokeArgs);
                        }

                        return dict;
                    }
                }

                if (genericType == typeof(Dictionary<,>))
                {
                    //todo: revisit, may be able to convert srcDict more easily

                    var sourceDict = source as Dictionary<string, object>;
                    if (sourceDict == null)
                        throw new InvalidCastException($"Type:{destType.Name} is a dictionary but '{source}' is of type:{sourceType.Name}");

                    var dict = Activator.CreateInstance(destType, sourceDict.Count);
                    var add = destType.GetMethod("Add");
                    
                    var invokeArgs = new object[2];
                    foreach (var pair in sourceDict)
                    {
                        invokeArgs[0] = Cast(genericArgs[0], pair.Key, context);
                        invokeArgs[1] = Cast(genericArgs[1], pair.Value, context);
                        add.Invoke(dict, invokeArgs);
                    }

                    return dict;
                }
            }

            //struct initialization using shorthand array syntax
            if (destTypeInfo.IsValueType && sourceList != null)
            {
                var obj = Activator.CreateInstance(destType);
                var members = destType.GetMembers(BindingFlags.Instance | BindingFlags.Public);
                var memberEnumerator = members.GetEnumerator();
                for (int i = 0; i < sourceList.Count; ++i)
                {
                    if (!memberEnumerator.MoveNext())
                        throw new ArgumentOutOfRangeException($"too many members when converting to {destType.Name}");

                    //parse dictionary (test if field or property and set accordingly)
                    if (memberEnumerator.Current is PropertyInfo p)
                    {
                        if (p.CanWrite)
                            p.SetValue(obj, Cast(p.PropertyType, sourceList[i], context));
                    }
                    else if (memberEnumerator.Current is FieldInfo f)
                    {
                        if (!f.IsInitOnly)
                            f.SetValue(obj, Cast(f.FieldType, sourceList[i], context));
                    }
                    else
                        --i; //does not seem to be built in way to filter out non-var members
                }
                return obj;
            }

            if (sourceType == typeof(Dictionary<string, object>))
            {
                var dest = CreateType(destType);
                return ParseDictionary(destType, dest, (Dictionary<string, object>)source, context);
            }

            if (sourceTypeInfo.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                var lazyVal = sourceType.GetProperty("Value").GetValue(source);
                return Cast(destType, lazyVal, context);
            }

            //implicit cast (limited support)
            {
                var implCast = destType.GetMethod("op_Implicit", new[] { sourceType });
                if (implCast != null)
                    return implCast.Invoke(null, new[] { source });
            }

            {
                if (TryNumericCast(source, out var destNumber, sourceTypeInfo, destTypeInfo))
                    return destNumber;
            }

            if (destType == typeof(string))
                return source.ToString();

            throw new InvalidCastException($"Error converting '{source}' from type:{sourceType.Name} to type:{destType.Name}");
        }

        public static bool TryNumericCast(object source, out object dest, TypeInfo sourceType, TypeInfo destType)
        {
            bool canConvert = false;

            bool isSourceInt = IsInt(sourceType);
            bool isSourceFloat = IsFloat(sourceType);

            bool isDestInt = IsInt(destType);
            bool isDestFloat = IsFloat(destType);

            canConvert |= (isDestInt && isSourceInt);
            canConvert |= (isDestFloat || isDestInt) && (isSourceFloat || isSourceInt);

            if (canConvert)
            {
                if (destType.IsEnum)
                    dest = Enum.ToObject(destType, source);

                dest = Convert.ChangeType(source, destType);
                return true;
            }
            dest = null;
            return false;
        }

        public static char FromLiteral(char escape)
        {
            switch (escape)
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
                    return escape;
            }
        }

        /// <summary>
        /// Read a string enclosed in quotes
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <returns>The string (without quotes)</returns>
        public static string ReadString(TextReader reader)
        {
            var builder = new StringBuilder();
            var end = reader.Read();
            while (reader.Peek() > 0 && reader.Peek() != end)
            {
                var ch = reader.Read();
                if (ch == '\\' && reader.Peek() > 0)
                    builder.Append(FromLiteral((char)reader.Read()));
                else
                    builder.Append((char)ch);
            }
            reader.Read();
//#if WINDOWS
            return string.Intern(builder.ToString());
//#else
            //return builder.ToString(); //todo: .net 2+ intern
//#endif
        }

        public static string ReadWord(TextReader reader)
        {
            var builder = new StringBuilder();
            char pk;
            while (reader.Peek() > 0 &&
                   !Char.IsSeparator(pk = (char)reader.Peek()) &&
                   !Char.IsPunctuation(pk))
                builder.Append((char)reader.Read());
            return builder.ToString();
        }

        /// <summary>
        /// Assumes immediately at a comment (Post SkipWhitespace)
        /// </summary>
        public static void SkipComments(TextReader reader)
        {
            var ch = reader.Peek();
            if (ch == -1 || ch != '#')
                return;

            reader.Read(); //skip #
            ch = reader.Read();

            if (ch == -1)
                return;

            //multiline comment #* .... *#
            if (ch == '*')
            {
                //read until *#
                while (((ch = reader.Read()) != -1 && ch != '*') ||
                       ((ch = reader.Read()) != -1 && ch != '#')) ;
            }
            else
                while ((ch = reader.Read()) != -1 && ch != '\n') ;
        }

        public static void SkipWhitespace(TextReader reader)
        {
            int ch;
            while ((ch = reader.Peek()) != -1 && Char.IsWhiteSpace((char)ch))
                reader.Read();
        }

        /// <summary>
        /// Skip comments and whitespace
        /// </summary>
        public static void SkipIgnored(TextReader reader)
        {
            SkipWhitespace(reader);
            while (reader.Peek() > 0 && reader.Peek() == '#')
            {
                SkipComments(reader);
                SkipWhitespace(reader);
            }
        }

        /// <summary>
        /// Apply an object's values to another object of the same type
        /// </summary>
        /// <param name="target">The object to apply to</param>
        /// <param name="source">The object to read from</param>
        public static void ApplyObject(object target, object source)
        {
            var type = target.GetType();
            if (type != source.GetType())
                throw new ArgumentException("Source object must be the same as target object");

            if (target is System.Collections.IList lt &&
                source is System.Collections.IList ls)
            {
                //todo: should cast/throw if source is not list

                lt.Clear();
                foreach (var v in ls)
                    lt.Add(v);
            }
            else if (target is System.Collections.IDictionary dt &&
                    source is System.Collections.IDictionary ds)
            {
                var ke = ds.Keys.GetEnumerator();
                var kv = ds.Values.GetEnumerator();
                dt.Clear();
                while (ke.MoveNext())
                {
                    kv.MoveNext();
                    dt.Add(ke.Current, kv.Current);
                }
            }
            else
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.CanWrite && prop.CanRead && !prop.IsDefined(typeof(IgnoredAttribute)))
                        prop.SetValue(target, prop.GetValue(source));
                }
                foreach (var val in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!val.IsInitOnly && !val.IsDefined(typeof(IgnoredAttribute)))
                        val.SetValue(target, val.GetValue(source));
                }
            }
        }
    }
}
