using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Data
{
    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class NonSerializedAttribute : Attribute { }

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
    public static partial class Serializer
    {
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
            RegisteredTypes = new Dictionary<string, Type>();
            Serializers = new Dictionary<Type, CustomTypeSerializer>();

            LoadTypesFrom(Assembly.GetExecutingAssembly());
            RegisteredTypes.Add(typeof(PlayerIndex).Name, typeof(PlayerIndex));

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

            Serializers.Add(typeof(TimeSpan), new CustomTypeSerializer
            {
                Serialize = (object Value) => { return ((TimeSpan)Value).TotalMilliseconds; },
                Deserialize = (object Value) => { return TimeSpan.FromMilliseconds((double)Convert.ChangeType(Value, typeof(double))); }
            });

            Serializers.Add(typeof(BlendState), new CustomTypeSerializer
            {
                Serialize = (object Value) =>
                {
                    if (Value == BlendState.Additive)
                        return "Additive";
                    if (Value == BlendState.AlphaBlend)
                        return "AlphaBlend";
                    if (Value == BlendState.NonPremultiplied)
                        return "NonPremultiplied";
                    if (Value == BlendState.Opaque)
                        return "Opaque";

                    return "todo";
                },
                Deserialize = (object Value) =>
                {
                    if (Value is string strValue)
                    {
                        switch ((string)Value)
                        {
                            case "Additive":
                                return BlendState.Additive;
                            case "AlphaBlend":
                                return BlendState.AlphaBlend;
                            case "NonPremultiplied":
                                return BlendState.NonPremultiplied;
                            case "Opaque":
                                return BlendState.Opaque;
                            default:
                                return null;
                        }
                    }

                    return null;
                }
            });
        }

        /// <summary>
        /// Load types from an assembly
        /// </summary>
        /// <param name="Assembly">The assembly to load from</param>
        public static void LoadTypesFrom(Assembly Ass)
        {
            if (Ass == null)
                return;

            Type[] types = Ass.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsGenericType && !Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                    RegisteredTypes[WriteFullTypeNames ? type.FullName : type.Name] = type;
            }
        }
    }
}
