using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
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

        public const bool WriteFullTypeNames = false;
        public const bool CaseSensitiveMembers = false;

        //cached types from assemblies
        public static Dictionary<string, Type> RegisteredTypes { get; set; }

        /// <summary>
        /// Custom serializers (provided for things like system classes. User defined classes can use CustomSerializeAttribute
        /// </summary>
        public static Dictionary<Type, CustomTypeSerializer> Serializers { get; set; }

        static Serializer()
        {
            RegisteredTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            Serializers = new Dictionary<Type, CustomTypeSerializer>();

            LoadTypesFrom(Assembly.GetExecutingAssembly());

            //default custom serializers

            Serializers.Add(typeof(Vector2), new CustomTypeSerializer
            {
                Serialize = (object value) => LinearStruct,
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var x = (float)Convert.ChangeType(v[0], typeof(float));
                    var y = (float)Convert.ChangeType(v[1], typeof(float));
                    return new Vector2(x, y);
                }
            });

            Serializers.Add(typeof(Vector3), new CustomTypeSerializer
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
            });

            Serializers.Add(typeof(Vector4), new CustomTypeSerializer
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
            });

            Serializers.Add(typeof(Point), new CustomTypeSerializer
            {
                Serialize = (object value) => LinearStruct,
                Deserialize = (object value, DeserializationContext cxt) =>
                {
                    var v = (List<object>)value;
                    var x = (int)Convert.ChangeType(v[0], typeof(int));
                    var y = (int)Convert.ChangeType(v[1], typeof(int));
                    return new Point(x, y);
                }
            });

            Serializers.Add(typeof(Rectangle), new CustomTypeSerializer
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
            });

            Serializers.Add(typeof(Color), new CustomTypeSerializer
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
            });

            Serializers.Add(typeof(Curve), new CustomTypeSerializer
            {
                Deserialize = (object value, DeserializationContext cxt) => {
                    return null; //todo
                },
            });

            Serializers.Add(typeof(Texture2D), new CustomTypeSerializer
            {
                Serialize = (object value) => { return ((Texture2D)value).Name; },
                Deserialize = (object value, DeserializationContext cxt) => { return Cache.Load<Texture2D>((string)value, cxt.root); } //todo: pass in serialization context
            });

            Serializers.Add(typeof(SoundEffect), new CustomTypeSerializer
            {
                Serialize = (object value) => { return ((SoundEffect)value).Name; },
                Deserialize = (object value, DeserializationContext cxt) => { return Cache.Load<SoundEffect>((string)value, cxt.root); }
            });

            Serializers.Add(typeof(TimeSpan), new CustomTypeSerializer
            {
                Serialize = (object value) => { return ((TimeSpan)value).TotalMilliseconds; },
                Deserialize = (object value, DeserializationContext cxt) => { return TimeSpan.FromMilliseconds((double)Convert.ChangeType(value, typeof(double))); }
            });

            Serializers.Add(typeof(BlendState), new CustomTypeSerializer
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
                },
            });
        }

        public static void LoadRunningAssemblyTypes()
        {
            LoadTypesFrom(Assembly.GetEntryAssembly());
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
                if (!type.IsGenericType &&
                    !Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false) &&
                    !Attribute.IsDefined(type, typeof(IgnoredAttribute), true))
                    RegisterType(type);
            }

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

            string name;
            if (mtype.IsEnum)
            {
                name = WriteFullTypeNames ? mtype.FullName : mtype.Name;
                bool isFlags = mtype.GetCustomAttribute(typeof(FlagsAttribute)) != null;
                return name + $"{(isFlags ? "[" : "(")}{string.Join(" ", Enum.GetNames(mtype))}{(isFlags ? "]" : ")")}"; //todo
            }
            else if (mtype.IsGenericType)
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

            if (type.IsEnum)
            {
                foreach (var val in type.GetEnumValues())
                    dict[type.GetEnumName(val)] = Convert.ToInt64(val);
                return dict;
            }

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(member, typeof(IgnoredAttribute)))
                    continue;

                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        dict[member.Name] = DescribeMemberType(((FieldInfo)member).FieldType);
                        break;
                    case MemberTypes.Property:
                        if (((PropertyInfo)member).CanWrite)
                            dict[member.Name] = DescribeMemberType(((PropertyInfo)member).PropertyType);
                        break;
                    default:
                        break;
                }
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
