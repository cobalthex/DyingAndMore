﻿using System;
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
    /// Allow this object to be serialized in debug mode, regardless of write settings
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DebugSerializeAttribute : Attribute { }

    /// <summary>
    /// This member/object should be serizlied with the specified (instance) method
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomSerializeAttribute : Attribute
    {
        public const BindingFlags Flags = 0
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.IgnoreCase;

        internal string methodName;

        /// <summary>
        /// Create a custom serializer
        /// </summary>
        /// <param name="Type">The type containing the method to use for serializing</param>
        /// <param name="MethodName">The name of the method</param>
        /// <param name="OverrideSerializeType">If the object returned is a dictionary, export it as type <see cref="Type"/></param>
        public CustomSerializeAttribute(string methodName)
        {
            this.methodName = methodName;
        }
    }

    public interface IDerivedSerialize
    {
        Dictionary<string, object> DerivedSerialize();
    }

    public static partial class Serializer
    {
        /// <summary>
        /// Specifies that a custom serializer should serialize the object to an ordered array of all members
        /// </summary>
        public static readonly object LinearStruct = new object();

        public static void TextSerialize(string file, object serializing)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            using (var writer = new StreamWriter(new FileStream(file, FileMode.Create)))
            {
                writer.BaseStream.SetLength(0); // truncate
                TextSerialize(writer, serializing);
            }
        }

        public static string TextSerializeToString(object serializing)
        {
            var sw = new StringWriter();
            TextSerialize(sw, serializing);
            return sw.ToString();
        }

        /// <summary>
        /// Serialize an object to a text stream
        /// </summary>
        /// <param name="writer">The stream to write to</param>
        /// <param name="serializing">The object to serialize</param>
        /// <remarks>Some data types (Takai/Xna) are custom serialized</remarks>
        public static void TextSerialize(TextWriter writer, object serializing, int indentLevel = 0, bool serializeExternals = false, bool serializeNonPublics = false, bool serializeDebuggables = false, bool asReference = false)
        {
            if (serializing == null)
            {
                writer.Write("Null");
                return;
            }

            var ty = serializing.GetType();

            //external serialization
            if (!serializeExternals && serializing is ISerializeExternally sex)
            {
                if (!string.IsNullOrEmpty(sex.File))
                {
                    writer.Write($"@\"{sex.File}\"");
                    return;
                }
            }

            if (asReference && serializing is IReferenceable ir)
            {
                if (ir.Name == null)
                    ir.Name = Util.RandomString(8, 8, "ref_");
                writer.Write($"*{(WriteFullTypeNames ? ty.FullName : ty.Name)}.{ir.Name}"); //todo: serialize externals?
                return;
            }

            var typeInfo = ty.GetTypeInfo();

            var custSerial = typeInfo.GetCustomAttribute<CustomSerializeAttribute>(true);

            if (typeInfo.IsPrimitive)
                writer.Write(serializing);

            else if (typeInfo.IsEnum)
            {
                writer.Write(WriteFullTypeNames ? typeInfo.FullName : ty.Name);

                if (typeInfo.IsDefined(typeof(FlagsAttribute)) && Convert.ToUInt64(serializing) != 0)
                {
                    writer.Write(" [");
                    var e = serializing as Enum;
                    var enumValues = Enum.GetNames(ty);
                    int n = 0;
                    foreach (var flag in enumValues)
                    {
                        var value = (Enum)Enum.Parse(ty, flag);
                        if ((UInt64)Convert.ToInt64(value) != 0 && e.HasFlag(value)) //allow uint wraparound
                        {
                            if (n++ > 0)
                                writer.Write(' ');
                            writer.Write(flag);
                        }
                    }
                    writer.Write("]");
                }
                else
                    writer.Write(".{0}", serializing.ToString());
            }

            else if (ty == typeof(string) || ty == typeof(char[]))
                writer.Write(serializing.ToString().ToLiteral());

            //user-defined serializer
            else if (custSerial?.methodName != null)
            {
                var method = ty.GetMethod(custSerial.methodName, CustomSerializeAttribute.Flags);
                var serialized = method?.Invoke(serializing, null);
                if (serialized == LinearStruct)
                    SerializeLinear(writer, serializing, serializeExternals, serializeNonPublics, serializeDebuggables);
                else
                    TextSerialize(writer, serialized, indentLevel, serializeExternals, serializeNonPublics, serializeDebuggables);
            }

            //todo: maybe remove and just add a bunch of custom serializers
            //custom serializer
            else if (Serializers.ContainsKey(ty) && Serializers[ty].Serialize != null)
            {
                var serialized = Serializers[ty].Serialize(serializing);
                if (serialized == LinearStruct)
                    SerializeLinear(writer, serializing, serializeExternals, serializeNonPublics, serializeDebuggables);
                else
                    TextSerialize(writer, serialized, indentLevel, serializeExternals, serializeNonPublics, serializeDebuggables);
            }

            else if (typeof(IDictionary).IsAssignableFrom(ty))
            {
                writer.WriteLine("{");

                var dict = (IDictionary)serializing;
                var keyType = ty.GetGenericArguments()[0];
                foreach (var key in dict.Keys)
                {
                    Indent(writer, indentLevel + 1);

                    if (keyType == typeof(char))
                        writer.Write((UInt16)(char)key);
                    else
                        writer.Write(key.ToString());

                    writer.Write(": ");
                    TextSerialize(writer, dict[key], indentLevel + 1, serializeExternals, serializeNonPublics, serializeDebuggables, asReference: asReference);
                    writer.WriteLine(";");
                }

                Indent(writer, indentLevel);
                writer.Write("}");
            }

            else if (typeof(IEnumerable).IsAssignableFrom(ty))
            {
                writer.Write("[");
                bool once = false, lastWasSerializer = false;
                foreach (var i in (IEnumerable)serializing)
                {
                    lastWasSerializer = false;
                    if (i == null || !i.GetType().GetTypeInfo().IsPrimitive)
                    {
                        writer.WriteLine();
                        Indent(writer, indentLevel + 1);
                        lastWasSerializer = true;
                    }
                    else if (once)
                        writer.Write(' ');
                    once = true;

                    TextSerialize(writer, i, indentLevel + 1, serializeExternals, serializeNonPublics, serializeDebuggables,
                        asReference: asReference);
                }
                if (lastWasSerializer)
                {
                    writer.WriteLine();
                    Indent(writer, indentLevel);
                }
                writer.Write("]");
            }

            else
            {
                var typeName = WriteFullTypeNames ? ty.FullName : ty.Name;
                writer.WriteLine($"{typeName} {{");

                foreach (var prop in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance | (serializeNonPublics ? BindingFlags.NonPublic : 0)))
                {
                    if ((prop.IsDefined(typeof(DebugSerializeAttribute)) ||
                         prop.IsDefined(typeof(CustomDeserializeAttribute)) ||
                         prop.GetSetMethod() != null || (serializeNonPublics && prop.CanWrite))
                        && prop.CanRead)
                    {
                        SerializeMember(
                            writer,
                            serializing, 
                            prop,
                            prop.GetValue(serializing), 
                            prop.PropertyType,
                            indentLevel, 
                            serializeExternals, 
                            serializeNonPublics, 
                            serializeDebuggables
                        );
                    }
                }

                foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance | (serializeNonPublics ? BindingFlags.NonPublic : 0)))
                {
                    if (field.IsInitOnly && !field.IsDefined(typeof(DebugSerializeAttribute)))
                        continue;

                    SerializeMember(
                        writer,
                        serializing, 
                        field,
                        field.GetValue(serializing),
                        field.FieldType, 
                        indentLevel, 
                        serializeExternals,
                        serializeNonPublics,
                        serializeDebuggables
                    );
                }

                if (serializing is IDerivedSerialize derived)
                {
                    var props = derived.DerivedSerialize();
                    if (props != null)
                    {
                        foreach (var prop in props)
                        {
                            Indent(writer, indentLevel + 1);
                            writer.Write("{0}: ", prop.Key);
                            TextSerialize(writer, prop.Value, indentLevel + 1, serializeExternals, serializeNonPublics, serializeDebuggables);
                            writer.WriteLine(";");
                        }
                    }
                }

                Indent(writer, indentLevel);
                writer.Write("}");
            }
        }

        private static void SerializeMember(TextWriter writer, object parent, MemberInfo member, object value, Type valueType, int indentLevel, bool serializeExternals, bool serializeNonPublics, bool serializeDebuggables)
        {
            if (member.IsDefined(typeof(IgnoredAttribute)) || member.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
                return;

            Indent(writer, indentLevel + 1);
            writer.Write("{0}: ", member.Name);

            if (value == null)
                writer.Write("Null");
            else if (member.IsDefined(typeof(AsReferenceAttribute)))
            {
                if (value is IReferenceable ir)
                {
                    if (ir.Name == null)
                        ir.Name = Util.RandomString(8, 8, "ref_");
                    writer.Write($"*{(WriteFullTypeNames ? valueType.FullName : valueType.Name)}.{ir.Name}"); //todo:  serialize externals?
                }
                else
                {
                    TextSerialize(writer, value, indentLevel + 1, serializeExternals, serializeNonPublics, serializeDebuggables,
                        asReference: true);
                }
            }
            else
            {
                //custom-defined serializer
                var custSerial = member.GetCustomAttribute<CustomSerializeAttribute>(true);
                if (custSerial?.methodName != null)
                {
                    var method = member.DeclaringType.GetMethod(custSerial.methodName, CustomSerializeAttribute.Flags);
                    TextSerialize(writer, method.Invoke(parent, null), indentLevel + 1, serializeExternals, serializeNonPublics, serializeDebuggables);
                }
                else
                    TextSerialize(writer, value, indentLevel + 1, serializeExternals, serializeNonPublics, serializeDebuggables);
            }
            writer.WriteLine(";");
        }

        private static void SerializeLinear(TextWriter writer, object serializing, bool serializeExternals, bool serializeNonPublics, bool serializeDebuggables)
        {
            writer.Write('[');
            var ty = serializing.GetType();
            int n = 0;
            foreach (var member in ty.GetMembers(BindingFlags.Instance | BindingFlags.Public | (serializeNonPublics ? BindingFlags.NonPublic : 0)))
            {
                if (member is PropertyInfo p)
                {
                    if (!p.CanWrite || !p.CanRead ||
                        (p.GetSetMethod() == null && !serializeNonPublics
                         && !p.IsDefined(typeof(CustomDeserializeAttribute))))
                        continue;

                    if (n++ > 0)
                        writer.Write(' ');

                    TextSerialize(writer, p.GetValue(serializing), 0, serializeExternals, serializeNonPublics, serializeDebuggables);
                }
                else if (member is FieldInfo f)
                {
                    if (f.IsInitOnly)
                        continue;

                    if (n++ > 0)
                        writer.Write(' ');

                    TextSerialize(writer, f.GetValue(serializing), 0, serializeExternals, serializeNonPublics, serializeDebuggables);
                }
            }
            writer.Write(']');
        }

        private static void Indent(TextWriter writer, int indent)
        {
            writer.Write(new string(' ', indent * 4));
        }

        private static string ToLiteral(this string input)
        {
            var literal = new StringBuilder(input.Length + 2);
            literal.Append("\"");
            foreach (var c in input)
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
