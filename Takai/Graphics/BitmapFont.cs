﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO.Compression;
#if !XBOX
using System;
#endif

namespace Takai.Graphics
{
    [Data.CustomDeserialize(typeof(BitmapFont), "DeserializeFont")]
    public class BitmapFont : IDisposable, Data.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        /// <summary>
        /// All of the available characters in the font
        /// </summary>
        public System.Collections.Generic.Dictionary<char, Rectangle> Characters
        {
            get => characters;
            protected set
            {
                characters = value;
                MaxCharWidth = 0;
                MaxCharHeight = 0;
                foreach (var @char in characters)
                {
                    MaxCharWidth  = Math.Max(MaxCharWidth, @char.Value.Width);
                    MaxCharHeight = Math.Max(MaxCharHeight, @char.Value.Height);
                }
            }
        }
        private System.Collections.Generic.Dictionary<char, Rectangle> characters;

        /// <summary>
        /// The source texture holding all of the character images
        /// </summary>
        public Texture2D Texture { get; protected set; }
        /// <summary>
        /// Spacing between each character when drawn
        /// </summary>
        public Point Tracking { get; set; }

        /// <summary>
        /// The maximum width of all characters
        /// </summary>
        [Data.Serializer.Ignored]
        public int MaxCharWidth { get; protected set; }

        /// <summary>
        /// The maximum height of all characters
        /// </summary>
        [Data.Serializer.Ignored]
        public int MaxCharHeight { get; protected set; }

        private static object DeserializeFont(object transform)
        {
            if (transform is string file)
                return Data.Cache.Load<BitmapFont>(file);

            return Data.Serializer.DefaultAction;
        }

        /// <summary>
        /// Load a bitmap font from a stream
        /// </summary>
        /// <param name="stream">The stream to load from</param>
        /// <returns>The bitmap font if created, null if not</returns>
        public static BitmapFont FromStream(GraphicsDevice graphicsDevice, System.IO.Stream stream)
        {
            BitmapFont font = new BitmapFont()
            {
                Tracking = Point.Zero
            };
            var read = new DeflateStream(stream, CompressionMode.Decompress);

            //broken up because of flipped endians on xbox
            byte[] block = new byte[4];
            int x, y, w, h; char c;
            read.Read(block, 0, 4);
            int len = BitConverter.ToInt32(block, 0);
            font.characters = new System.Collections.Generic.Dictionary<char, Rectangle>(len);
            font.MaxCharWidth = 0;
            font.MaxCharHeight = 0;
            for (int i = 0; i < len; ++i)
            {
#if XBOX
                read.Read(block, 2, 2); c = BitConverter.ToChar(block, 0);
#else
                read.Read(block, 0, 2); c = BitConverter.ToChar(block, 0);
#endif
                read.Read(block, 0, 4); x = BitConverter.ToInt32(block, 0);
                read.Read(block, 0, 4); y = BitConverter.ToInt32(block, 0);
                read.Read(block, 0, 4); w = BitConverter.ToInt32(block, 0);
                read.Read(block, 0, 4); h = BitConverter.ToInt32(block, 0);

                font.MaxCharWidth = Math.Max(font.MaxCharWidth, w);
                font.MaxCharHeight = Math.Max(font.MaxCharHeight, h);

                font.characters.Add(c, new Rectangle(x, y, w, h));
            }
            block = new byte[8];
            read.Read(block, 0, 8);
            long length = BitConverter.ToInt64(block, 0);
            System.IO.MemoryStream ms = new System.IO.MemoryStream((int)length);

            int sz = 4096;
            var buf = new byte[sz];
            int red = 0;
            do
            {
                red = read.Read(buf, 0, sz);
                ms.Write(buf, 0, red);
            } while (red > 0);

            ms.Seek(0, System.IO.SeekOrigin.Begin);
            font.Texture = Texture2D.FromStream(graphicsDevice, ms);

            return font;
        }

        /// <summary>
        /// Dispose of this font
        /// </summary>
        public void Dispose()
        {
            Texture.Dispose();
            Tracking = Point.Zero;
            Characters.Clear();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Draw a string
        /// </summary>
        /// <param name="spriteBatch">Spritebatch to use</param>
        /// <param name="text">The string to draw (New lines included)</param>
        /// <param name="position">Where to draw the string</param>
        /// <param name="color">The hue to draw the string with</param>
        /// <param name="monospace">Draw the font, treating all characters as equal width</param>
        /// <remarks>The escape sequence \n will create a new line (left alignment). You can also type `rgb as a 3 char number between 000 and www (base 33) to set the color in RGB. use `x to reset the color</remarks>
        /// <returns>Returns the size of the text drawn</returns>
        public Vector2 Draw(SpriteBatch spriteBatch, string text, Vector2 position, Color color, bool monospace = false)
        {
            if (text == null)
                return Vector2.Zero;

            var pos = position.ToPoint().ToVector2(); //round down to nearest int
            Color curColor = color;

            int maxH = 0;

            for (int i = 0; i < text.Length; ++i)
            {
                char ch = text[i];

                //newline
                if (ch == '\n')
                {
                    pos.X = (int)position.X;
                    pos.Y += maxH + Tracking.Y;
                    maxH = 0;
                    continue;
                }
                else if (ch == '`' && i + 1 < text.Length) //colors (`RGB (hex)) and `x to end
                {
                    if (text[i + 1] == 'x')
                    {
                        curColor = color;
                        ++i;
                        continue;
                    }
                    else if (i + 3 < text.Length)
                    {
                        int[] col = new int[3];
                        for (int j = 0; j < 3; ++j, i++)
                        {
                            char cch = text[i + 1];
                            if (cch >= 'a' && cch <= 'f')
                                col[j] = 10 + (cch - 'a');
                            else if (cch >= 'A' && cch <= 'F')
                                col[j] = 10 + (cch - 'A');
                            else if (cch >= '0' && cch <= '9')
                                col[j] = (cch - '0');
                        }
                        curColor = new Color(col[0] << 4, col[1] << 4, col[2] << 4);
                        continue;
                    }
                }

                if (!Characters.TryGetValue(ch, out var rgn))
                    continue;

                spriteBatch.Draw(Texture, pos, rgn, curColor);

                pos.X += (monospace ? MaxCharWidth : rgn.Width) + Tracking.X;
                maxH = Math.Max(maxH, rgn.Height); //todo: maybe use MaxCharHeight
            }

            return pos - position;
        }

        /// <summary>
        /// Draw a string in a specific region
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use</param>
        /// <param name="text">The string to draw</param>
        /// <param name="bounds">The clipping bounds and location</param>
        /// <param name="color">The default font color</param>
        /// <param name="monoSpace">Draw the font, treating all characters as equal width</param>
        /// <remarks>The escape sequence \n will create a new line (left alignment). You can also type `rgb as a 3 char number between 000 and www (base 33) to set the color in RGB. use `x to reset the color</remarks>
        public void Draw(SpriteBatch spriteBatch, string text, Rectangle bounds, Color color, bool monoSpace = false)
        {
            Draw(spriteBatch, text, 0, -1, bounds, Point.Zero, color, monoSpace);
        }

        /// <summary>
        /// Draw a variable length string in a specific region with a given offset
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use</param>
        /// <param name="text">The string to draw</param>
        /// <param name="start">The zero-based starting position in the string</param>
        /// <param name="length">The length of the sub string, use -1 for the entire string (non-printing characters not included)</param>
        /// <param name="bounds">The clipping region for drawing the text to</param>
        /// <param name="offset">The pixel offset to render the text inside the clipping region</param>
        /// <param name="color">The color of the text</param>
        /// <param name="monoSpace">Draw the font, treating all characters as equal width</param>
        /// <remarks>The escape sequence \n will create a new line (left alignment). You can also type `rgb as a 3 char number between 000 and fff to change the color. use `x to reset the color</remarks>
        public void Draw(SpriteBatch spriteBatch, string text, int start, int length, Rectangle bounds, Point offset, Color color, bool monoSpace = false)
        {
            if (spriteBatch == null || text == null)
                return;

            Point pos = offset;
            Color curColor = color;

            int lineHeight = 0;

            if (length == -1)
                length = text.Length;

            for (int i = start; i < Math.Min(text.Length, start + length); ++i)
            {
                char ch = text[i];

                //newline
                if (ch == '\n')
                {
                    pos.X = offset.X;
                    pos.Y += lineHeight + Tracking.Y;
                    lineHeight = 0;
                    continue;
                }
                else if (ch == '`' && i + 1 < length) //colors (`RGB (hex)) and `x to end
                {
                    if (text[i + 1] == 'x')
                    {
                        curColor = color;
                        length += 2;
                        ++i;
                        continue;
                    }
                    else if (i + 3 < text.Length)
                    {
                        int[] col = new int[3];
                        for (int j = 0; j < 3; ++j)
                        {
                            char cch = text[i + j + 1];
                            if (cch >= '0' && cch <= '9')
                                col[j] = (cch - '0');
                            else if (cch >= 'a' && cch <= 'f')
                                col[j] = 10 + (cch - 'a');
                            else if (cch >= 'A' && cch <= 'F')
                                col[j] = 10 + (cch - 'A');
                        }
                        curColor = new Color(col[0] << 4, col[1] << 4, col[2] << 4);
                        i += 3;
                        length += 4;
                        continue;
                    }
                }

                else if (pos.Y > bounds.Height)
                    return;
                else if (pos.X >= bounds.Width)
                    continue;

                if (!Characters.TryGetValue(ch, out var rgn))
                    continue;

                //outline each character's bounds
                //Primitives2D.DrawRect(spriteBatch, Color.Coral, new Rectangle(bounds.Location + pos, rgn.Size));

                var loc = rgn.Location - pos;
                var clip = Rectangle.Intersect(rgn, new Rectangle(loc.X, loc.Y, bounds.Width, bounds.Height));

                if (clip.Width > 0 && clip.Height > 0)
                {
                    loc = bounds.Location + pos + (clip.Location - rgn.Location);
                    spriteBatch.Draw(
                        Texture,
                        new Rectangle(loc.X, loc.Y, clip.Width, clip.Height),
                        clip,
                        curColor
                    );
                }

                pos.X += (monoSpace ? MaxCharWidth : rgn.Width) + Tracking.X;
                lineHeight = Math.Max(lineHeight, rgn.Height);
            }
        }

        /// <summary>
        /// Measure a string for its size in pixels
        /// </summary>
        /// <param name="String">the string to measure</param>
        /// <returns>The size in pixels of the string</returns>
        /// <remarks>Characters not 6in the font are ignored</remarks>
        public Vector2 MeasureString(string String)
        {
            return MeasureString(String, 0, -1);
        }

        /// <summary>
        /// Measure a substring for its size in pixels
        /// </summary>
        /// <param name="text">The string to measure</param>
        /// <param name="startIndex">The zero-based start index in the string</param>
        /// <param name="length">The length of the sub string, use -1 for the entire string</param>
        /// <returns>The length of the string in pixels</returns>
        /// <remarks>Characters not in the font are ignored, escape characters are ignored aswell</remarks>
        public Vector2 MeasureString(string text, int startIndex, int length)
        {
            if (text == null)
                return Vector2.Zero;

            Vector2 total = Vector2.Zero;
            Vector2 row = Vector2.Zero;

            int start = startIndex;

            if (length == -1)
                length = text.Length;

            for (int i = start; i < Math.Min(text.Length, start + length); ++i)
            {
                if (text[i] == '\n')
                {
                    total.X = Math.Max(total.X, row.X);
                    total.Y += row.Y + Tracking.Y;
                    row = Vector2.Zero;
                    continue;
                }
                else if (text[i] == '`' && i + 1 < length) //colors (`RGB (hex)) and `x to end
                {
                    if (text[i + 1] == 'x')
                    {
                        ++i;
                        length += 2;
                        continue;
                    }
                    else if (i + 3 < text.Length)
                    {
                        i += 3;
                        length += 4;
                        continue;
                    }
                }

                if (Characters.TryGetValue(text[i], out var rgn))
                {
                    row.X += rgn.Width + Tracking.X;
                    row.Y  = Math.Max(row.Y, rgn.Height);
                }
            }

            total.X = Math.Max(total.X, row.X);
            total.Y += row.Y;
            return total;
        }
    }
}
