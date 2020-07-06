using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Takai.Data
{
    /// <summary>
    /// Specifies that this object can be serialized to a file. Used by serializer w/ <see cref="SerializeExternallyAttribute"/>
    /// </summary>
    public interface ISerializeExternally
    {
        /// <summary>
        /// The file that this class was loaded from. Null if no file
        /// </summary>
        [Serializer.Ignored]
        string File { get; set; }
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
        public Func<object, Serializer.DeserializationContext, object> Deserialize;
    }

    /// <summary>
    /// Serialize objects
    /// </summary>
    public static partial class Serializer
    {
        /// <summary>
        /// This value should be ignored by the serializer and deserializer
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = true)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public class IgnoredAttribute : Attribute { }

        /// <summary>
        /// This value should not be serialized, but can still be deserialized
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = true)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public class ReadOnlyAttribute : Attribute { }

        /// <summary>
        /// Throw an exception if this property is not set upon deserialization
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public class RequiredAttribute : Attribute { public RequiredAttribute() { throw new NotImplementedException(); } } //todo

        /// <summary>
        /// Store the value (or values if enumerable) as a reference to an object defined elsewhere.
        /// Reference must implement IReferenceable
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public class AsReferenceAttribute : Attribute { }

        //todo: serialize if has value?

        public const bool WriteFullTypeNames = false;
        public const bool CaseSensitiveIdentifiers = false;

        //cached types from assemblies
        public static Dictionary<string, Type> RegisteredTypes { get; set; }

        /// <summary>
        /// Custom serializers (provided for things like system classes. User defined classes can use CustomSerializeAttribute
        /// </summary>
        public static Dictionary<Type, CustomTypeSerializer> Serializers { get; set; }

        static Serializer()
        {
            RegisteredTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            LoadTypesFrom(typeof(Serializer).GetTypeInfo().Assembly);

            Serializers = new Dictionary<Type, CustomTypeSerializer>();
            LoadXnaTypes();
        }

        public static void LoadXnaTypes()
        {
            RegisterType<PlayerIndex>();
            RegisterType<BlendState>();

            RegisterType<Curve>();
            RegisterType<CurveKey>();
            RegisterType<CurveContinuity>();
            RegisterType<CurveLoopType>();

            RegisterType<Color>();
            RegisterType<Vector2>();
            RegisterType<Vector3>();
            RegisterType<Vector4>();
            RegisterType<Rectangle>();

            RegisterType<BlendState>();
            RegisterType<BlendFunction>();
            RegisterType<Blend>();

            RegisterType<Microsoft.Xna.Framework.Input.Buttons>();
            RegisterType<Microsoft.Xna.Framework.Input.Keys>();

            Serializers[typeof(Vector2)] = new CustomTypeSerializer
            {
                Serialize = (object value) => LinearStruct,
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    if (value is double d)
                        return new Vector2((float)d);
                    if (value is long l)
                        return new Vector2((float)l);
                    return DefaultAction;
                }
            };

            Serializers[typeof(Vector3)] = new CustomTypeSerializer
            {
                Serialize = (object value) => LinearStruct,
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var x = (float)Convert.ChangeType(v[0], typeof(float));
                    var y = (float)Convert.ChangeType(v[1], typeof(float));
                    var z = (float)Convert.ChangeType(v[2], typeof(float));
                    return new Vector3(x, y, z);
                }
            };

            Serializers[typeof(Vector4)] = new CustomTypeSerializer
            {
                Serialize = (object value) => LinearStruct,
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var x = (float)Convert.ChangeType(v[0], typeof(float));
                    var y = (float)Convert.ChangeType(v[1], typeof(float));
                    var z = (float)Convert.ChangeType(v[2], typeof(float));
                    var w = (float)Convert.ChangeType(v[3], typeof(float));
                    return new Vector4(x, y, z, w);
                }
            };

            Serializers[typeof(Point)] = new CustomTypeSerializer
            {
                Serialize = (object value) => LinearStruct,
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var x = (int)Convert.ChangeType(v[0], typeof(int));
                    var y = (int)Convert.ChangeType(v[1], typeof(int));
                    return new Point(x, y);
                }
            };

            Serializers[typeof(Rectangle)] = new CustomTypeSerializer
            {
                Serialize = (object value) => { var r = (Rectangle)value; return new[] { r.X, r.Y, r.Width, r.Height }; },
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var x = (int)Convert.ChangeType(v[0], typeof(int));
                    var y = (int)Convert.ChangeType(v[1], typeof(int));
                    var width = (int)Convert.ChangeType(v[2], typeof(int));
                    var height = (int)Convert.ChangeType(v[3], typeof(int));
                    return new Rectangle(x, y, width, height);
                }
            };

            Serializers[typeof(Color)] = new CustomTypeSerializer
            {
                Serialize = (object value) => { var c = (Color)value; return new[] { c.R, c.G, c.B, c.A }; },
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var r = (int)Convert.ChangeType(v[0], typeof(int));
                    var g = (int)Convert.ChangeType(v[1], typeof(int));
                    var b = (int)Convert.ChangeType(v[2], typeof(int));
                    var a = v.Count > 3 ? (int)Convert.ChangeType(v[3], typeof(int)) : 255;
                    return new Color(r, g, b, a);
                }
            };

            Serializers[typeof(Curve)] = new CustomTypeSerializer
            {
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    return null; //todo
                }
            };

            Serializers[typeof(Texture2D)] = new CustomTypeSerializer
            {
                Serialize = (object value) => { return ((Texture2D)value).Name; },
                Deserialize = (object value, DeserializationContext cxt) => { return Cache.Load<Texture2D>((string)value, cxt.root); } //todo: pass in serialization context
            };

            Serializers[typeof(SoundEffect)] = new CustomTypeSerializer
            {
                Serialize = (object value) => { return ((SoundEffect)value).Name; },
                Deserialize = (object value, DeserializationContext cxt) => { return Cache.Load<SoundEffect>((string)value, cxt.root); }
            };

            Serializers[typeof(TimeSpan)] = new CustomTypeSerializer
            {
                Serialize = (object value) => { return ((TimeSpan)value).TotalMilliseconds; },
                Deserialize = (object value, DeserializationContext cxt) => {
                    var dv = (double)Convert.ChangeType(value, typeof(double));
                    //negative/positive infinity -> Min/MaxValue?
                    return TimeSpan.FromMilliseconds(dv); 
                } //return suffix object?
            };

            Serializers[typeof(BlendState)] = new CustomTypeSerializer
            {
                Serialize = (object value) =>
                {
                    if (value == BlendState.Additive)
                        return "BlendState.Additive";
                    if (value == BlendState.AlphaBlend)
                        return "BlendState.AlphaBlend";
                    if (value == BlendState.NonPremultiplied)
                        return "BlendState.NonPremultiplied";
                    if (value == BlendState.Opaque)
                        return "BlendState.Opaque";

                    return DefaultAction;
                }
            };
        }

        /// <summary>
        /// Load types from an assembly
        /// </summary>
        /// <param name="Assembly">The assembly to load from</param>
        public static void LoadTypesFrom(Assembly assembly)
        {
            if (assembly == null)
                return;

            Type[] types = assembly.GetTypes();
            foreach (var type in types)
            {
                var typeInfo = type.GetTypeInfo();
                if (!type.GetTypeInfo().IsGenericType &&
                    !typeInfo.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) &&
                    !typeInfo.IsDefined(typeof(IgnoredAttribute), true))
                    RegisterType(type);
            }

        }

        public static void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }
        public static void RegisterType(Type type)
        {
            RegisteredTypes[WriteFullTypeNames ? type.FullName : type.Name] = type;
        }

        static string DescribeMemberType(Type mtype)
        {
            if (mtype == null)
                return null;

            var mTypeInfo = mtype.GetTypeInfo();

            string name;
            if (mTypeInfo.IsEnum)
            {
                name = WriteFullTypeNames ? mtype.FullName : mtype.Name;
                bool isFlags = mTypeInfo.IsDefined(typeof(FlagsAttribute));
                return name + $"{(isFlags ? "[" : "(")}{string.Join(" ", Enum.GetNames(mtype))}{(isFlags ? "]" : ")")}";
            }
            else if (mTypeInfo.IsGenericType)
            {
                name = WriteFullTypeNames ? mtype.FullName : mtype.Name;
                name = name.Substring(0, name.IndexOf('`')) + '<';
                for (int i = 0; i < mtype.GenericTypeArguments.Length; ++i)
                {
                    if (i > 0)
                        name += ',';
                    name += DescribeMemberType(mtype.GenericTypeArguments[i]);
                }
                return name + ">";
            }
            else
                name = WriteFullTypeNames ? mtype.FullName : mtype.Name;

            //pass in member info and check attributes

            return name;
        }

        public static Dictionary<string, object> DescribeType(Type type)
        {
            var dict = new Dictionary<string, object>();

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsEnum)
            {
                foreach (var val in Enum.GetValues(type))
                    dict[Enum.GetName(type, val)] = Convert.ToInt64(val);
                return dict;
            }

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (member.IsDefined(typeof(IgnoredAttribute)))
                    continue;

                if (member is PropertyInfo p)
                    dict[p.Name] = DescribeMemberType(p.PropertyType);
                else if (member is FieldInfo f)
                    dict[f.Name] = DescribeMemberType(f.FieldType);
            }

            //todo: handle derived +,custom *

            return dict;
        }

        static Dictionary<string, object> DescribeDictionary(Dictionary<string, object> dict)
        {
            var dest = new Dictionary<string, object>(dict.Count);
            foreach (var p in dict)
                dest[p.Key] = DescribeMemberType(p.Value.GetType());
            return dest;
        }
    }
}
