﻿using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
        
        private static string ExceptString(string BaseExceptionMessage, Stream Stream)
        {
            try { return BaseExceptionMessage + $" [Offset:{Stream.Position}]"; }
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
                            throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", Stream.BaseStream));

                        if (ch == ';' && key.Length == 0)
                        {
                            Stream.Read();
                            break;
                        }

                        key.Append((char)Stream.Read());
                    }
                    
                    if (Stream.Read() == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read object", Stream.BaseStream));

                    var value = TextDeserialize(Stream);
                    dict[key.ToString().TrimEnd()] = value;

                    SkipIgnored(Stream);

                    if ((ch = Stream.Peek()) == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected '}' to close object", Stream.BaseStream));

                    if (ch == ';')
                    {
                        Stream.Read(); //skip ;
                        SkipIgnored(Stream);
                    }
                    else if (ch != '}')
                        throw new InvalidDataException(ExceptString("Unexpected token. Expected ';' or '}' while trying to read object", Stream.BaseStream));
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
                            throw new InvalidDataException(ExceptString("Unexpected ';' in list (missing value)", Stream.BaseStream));
                    }

                    if (ch == -1)
                        throw new EndOfStreamException(ExceptString("Unexpected end of stream while trying to read list", Stream.BaseStream));

                    values.Add(TextDeserialize(Stream));
                    SkipIgnored(Stream);
                }

                if (Stream.Read() == -1) //skip ]
                    throw new EndOfStreamException(ExceptString("Unexpected end of stream. Expected ']' to close list", Stream.BaseStream));

                return values;
            }

            //file reference
            if (ch == '@')
                throw new NotImplementedException("Maybe later");

            if (ch == '"' || ch == '\'')
                return ReadString(Stream);

            string word;
            if (ch == '-' || ch == '+')
                word = (char)Stream.Read() + ReadWord(Stream);
            else
                word = ReadWord(Stream);

            if (Stream.Peek() == '.') //maybe handle ,
                word += (char)Stream.Read() + ReadWord(Stream);

            if (word.ToLower() == "null")
                return null;

            if (bool.TryParse(word, out var @bool))
                return @bool;
            
            if (long.TryParse(word, out var @int))
                return @int;

            if (double.TryParse(word, out var @float))
                return @float;
            
            if (RegisteredTypes.TryGetValue(word, out var type))
            {
                SkipIgnored(Stream);

                if (type.IsEnum)
                {
                    if (Stream.Peek() != '.')
                        throw new InvalidDataException(ExceptString($"Expected a '.' when reading enum value (EnumType.Key) while attempting to read '{word}'", Stream.BaseStream));

                    var eval = ReadWord(Stream);
                    return Enum.Parse(type, eval);
                }

                //read object
                if (Stream.Peek() != '{')
                    throw new InvalidDataException(ExceptString($"Expected an object definition when reading type '{word}' (Type {{ }})", Stream.BaseStream));
                
                var dict = TextDeserialize(Stream);

                throw new NotImplementedException("Todo: parsing dict into object");
            }

            throw new NotSupportedException(ExceptString($"Unknown identifier: '{word}'", Stream.BaseStream));
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
