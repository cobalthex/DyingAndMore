using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO.Compression;
#if !XBOX
using System;
#endif

namespace Takai.Graphics
{
    [Data.DesignerCreatable]
    public class BitmapFont : System.IDisposable
    {
        #region Data

        /// <summary>
        /// All of the available characters in the font
        /// </summary>
        public System.Collections.Generic.Dictionary<char, Rectangle> Characters { get; protected set; }

        /// <summary>
        /// The source texture holding all of the character images
        /// </summary>
        public Texture2D Texture { get; protected set; }
        /// <summary>
        /// Spacing between each character when drawn
        /// </summary>
        public Point Spacing { get; set; }

        /// <summary>
        /// The maximum width of all characters (for monospacing)
        /// </summary>
        public int MaxCharWidth { get; protected set; }

        #endregion

        #region Loading

        /// <summary>
        /// Load a bitmap font from a stream
        /// </summary>
        /// <param name="stream">The stream to load from</param>
        /// <returns>The bitmap font if created, null if not</returns>
        public static BitmapFont FromStream(GraphicsDevice GDev, System.IO.Stream Stream)
        {
            BitmapFont font = new BitmapFont();
            font.Spacing = Point.Zero;

            var read = new DeflateStream(Stream, CompressionMode.Decompress);

            //broken up because of flipped endians on xbox
            byte[] block = new byte[4];
            int x, y, w, h; char c;
            read.Read(block, 0, 4);
            int len = BitConverter.ToInt32(block, 0);
            font.Characters = new System.Collections.Generic.Dictionary<char, Rectangle>(len);
            font.MaxCharWidth = 0;
            for (int i = 0; i < len; i++)
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
                font.MaxCharWidth = MathHelper.Max(font.MaxCharWidth, w);
                font.Characters.Add(c, new Rectangle(x, y, w, h));
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
            font.Texture = Texture2D.FromStream(GDev, ms);

            read.Close();

            return font;
        }

        /// <summary>
        /// Dispose of this font
        /// </summary>
        public void Dispose()
        {
            Texture.Dispose();
            Spacing = Point.Zero;
            Characters.Clear();
            System.GC.SuppressFinalize(this);
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draw a string
        /// </summary>
        /// <param name="Spritebatch">Spritebatch to use</param>
        /// <param name="String">The string to draw (New lines included)</param>
        /// <param name="Position">Where to draw the string</param>
        /// <param name="Color">The hue to draw the string with</param>
        /// <param name="MonoSpace">Draw the font, treating all characters as equal width</param>
        /// <remarks>The escape sequence \n will create a new line (left alignment). You can also type `rgb as a 3 char number between 000 and www (base 33) to set the color in RGB. use `x to reset the color</remarks>
        public void Draw(SpriteBatch Spritebatch, string String, Vector2 Position, Color Color, bool MonoSpace = false)
        {
            if (String == null)
                return;

            Position.X = (int)Position.X;
            Position.Y = (int)Position.Y;

            Vector2 pos = Position;
            int maxH = 0;
            Color curColor = Color;
            int esc = 0;
            for (int i = 0; i < String.Length; i++)
            {
                //escape chars
                if (String[i] == '\n')
                {
                    pos.X = Position.X;
                    pos.Y += maxH + Spacing.Y;
                    maxH = 0;
                    esc++;
                    continue;
                }
                else if (String[i] == '`') //color
                {
                    if (String.Length > i + 1 && String[i + 1] == '`')
                        i++;
                    else if (String.Length > i + 1 && String[i + 1] == 'x')
                    {
                        curColor = Color;
                        i++;
                        continue;
                    }
                    else if (String.Length > i + 3)
                    {
                        int[] col = new int[3];
                        for (int j = 0; j < 3; j++)
                        {
                            char cch = String[j + i + 1];
                            if (cch >= 'a' && cch <= 'w')
                                col[j] = 10 + (int)(cch - 'a');
                            else if (cch >= 'A' && cch <= 'W')
                                col[j] = 10 + (int)(cch - 'A');
                            else if (cch >= '0' && cch <= '9')
                                col[j] = (int)(cch - '0');
                        }
                        curColor = new Color(col[0] << 3, col[1] << 3, col[2] << 3);
                        i += 3;
                        continue;
                    }
                }

                Rectangle rgn;
                if (!Characters.TryGetValue(String[i], out rgn))
                    continue;

                Spritebatch.Draw(Texture, pos, rgn, curColor);
                
                pos.X += (MonoSpace ? MaxCharWidth : rgn.Width) + Spacing.X;
                maxH = (int)MathHelper.Max(maxH, rgn.Height);
            }
        }

        /// <summary>
        /// Draw a string in a specific region
        /// </summary>
        /// <param name="Spritebatch">The sprite batch to use</param>
        /// <param name="String">The string to draw</param>
        /// <param name="Bounds">The clipping bounds and location</param>
        /// <param name="Color">The default font color</param>
        /// <param name="MonoSpace">Draw the font, treating all characters as equal width</param>
        /// <remarks>The escape sequence \n will create a new line (left alignment). You can also type `rgb as a 3 char number between 000 and www (base 33) to set the color in RGB. use `x to reset the color</remarks>
        public void Draw(SpriteBatch Spritebatch, string String, Rectangle Bounds, Color Color, bool MonoSpace = false)
        {
            Draw(Spritebatch, String, 0, -1, Bounds, Point.Zero, Color, MonoSpace);
        }

        /// <summary>
        /// Draw a variable length string in a specific region with a given offset
        /// </summary>
        /// <param name="Spritebatch">The sprite batch to use</param>
        /// <param name="String">The string to draw</param>
        /// <param name="StartIndex">The zero-based starting position in the string</param>
        /// <param name="Length">The length of the sub string, use -1 for the entire string (special chars not included)</param>
        /// <param name="Bounds">The bounds of the </param>
        /// <param name="Offset">The pixel offset of the text</param>
        /// <param name="Color">The color of the text</param>
        /// <param name="MonoSpace">Draw the font, treating all characters as equal width</param>
        /// <remarks>The escape sequence \n will create a new line (left alignment). You can also type `rgb as a 3 char number between 000 and www (base 33) to set the color in RGB. use `x to reset the color</remarks>
        public void Draw(SpriteBatch Spritebatch, string String, int StartIndex, int Length, Rectangle Bounds, Point Offset, Color Color, bool MonoSpace = false)
        {
            Vector2 pos = Vector2.Zero;
            Point off = Offset;
            Color curColor = Color;

            int maxH = 0;
            int start = GetStart(String, 0);

            int len = Length > String.Length ? String.Length : Length == -1 ? String.Length : Length;
            for (int i = 0; i + start < String.Length && i < len; i++)
            {
                char ch = String[i + start];
                if (ch == '\n')
                {
                    pos.X = Bounds.X;
                    pos.Y += maxH + Spacing.Y;
                    off.X = Offset.X;
                    off.Y -= maxH;
                    if (off.Y < 0)
                        off.Y = 0;
                    maxH = 0;
                    len++;
                    continue;
                }
                else if (ch == '`') //colors
                {
                    if (String.Length > i + start + 1 && String[i + start + 1] == '`')
                    {
                        i++;
                        len++;
                    }
                    else if (String.Length > i + start + 1 && String[i + start + 1] == 'x')
                    {
                        curColor = Color;
                        i++;
                        len += 2;
                        continue;
                    }
                    else if (String.Length > i + start + 3)
                    {
                        int[] col = new int[3];
                        for (int j = 0; j < 3; j++)
                        {
                            char cch = String[j + i + start + 1];
                            if (cch >= 'a' && cch <= 'w')
                                col[j] = 10 + (int)(cch - 'a');
                            else if (cch >= 'A' && cch <= 'W')
                                col[j] = 10 + (int)(cch - 'A');
                            else if (cch >= '0' && cch <= '9')
                                col[j] = (int)(cch - '0');
                        }
                        curColor = new Color(col[0] << 3, col[1] << 3, col[2] << 3);
                        i += 3;
                        len += 4;
                        continue;
                    }
                }

                Rectangle rgn;
                if (!Characters.TryGetValue(ch, out rgn))
                    continue;

                int ofx = off.X, ofy = off.Y;
                if (ofx < 0)
                    rgn.X -= ofx;
                else
                {
                    pos.X += ofx;
                    off.X = 0;
                }
                if (ofy < 0)
                    rgn.Y -= ofy;
                else
                {
                    pos.Y += ofy;
                    off.Y = 0;
                }

                rgn.Width = (Bounds.Width - ofx) > rgn.Width ? rgn.Width : Bounds.Width - ofx;
                rgn.Height = (Bounds.Height - ofy) > rgn.Height ? rgn.Height : Bounds.Height - ofy;

                int x = (int)pos.X, y = (int)pos.Y;

                if (rgn.Width < 1 || rgn.Height < 1 || x >= Bounds.Width || y >= Bounds.Height)
                    continue;

                int xdif = Bounds.Width - x, ydif = Bounds.Height - y;
                //x region check
                if (x + rgn.Width > Bounds.Width)
                    rgn.Width = xdif;
                else
                    rgn.Width = (rgn.Width > Bounds.Width ? Bounds.Width : rgn.Width);
                //y region check
                if (y + rgn.Height > Bounds.Height)
                    rgn.Height = ydif;
                else
                    rgn.Height = (rgn.Height > Bounds.Height ? Bounds.Height : rgn.Height);

                Spritebatch.Draw(Texture, new Vector2(Bounds.X + pos.X, Bounds.Y + pos.Y), rgn, curColor);

                pos.X += (MonoSpace ? MaxCharWidth : rgn.Width) + Spacing.X;
                maxH = (int)MathHelper.Max(maxH, rgn.Height);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Measure a string for its size in pixels
        /// </summary>
        /// <param name="String">the string to measure</param>
        /// <returns>The size in pixels of the string</returns>
        /// <remarks>Characters not in the font are ignored</remarks>
        public Vector2 MeasureString(string String)
        {
            return MeasureString(String, 0, -1);
        }

        /// <summary>
        /// Measure a substring for its size in pixels
        /// </summary>
        /// <param name="String">The string to measure</param>
        /// <param name="StartIndex">The zero-based start index in the string</param>
        /// <param name="Length">The length of the sub string, use -1 for the entire string</param>
        /// <returns>The length of the string in pixels</returns>
        /// <remarks>Characters not in the font are ignored, escape characters are ignored aswell</remarks>
        public Vector2 MeasureString(string String, int StartIndex, int Length)
        {
            Vector2 pos = Vector2.Zero;
            int nW = 0, nH = 0;

            int start = GetStart(String, StartIndex);

            int len = Length > String.Length ? String.Length : Length == -1 ? String.Length : Length;
            for (int i = 0; i + start < String.Length && i < len; i++)
            {
                if (String[i] == '\n')
                {
                    nW = 0;
                    pos.Y += nH + Spacing.Y;
                    nH = 0;
                    continue;
                }
                else if (String[i] == '`') //colors
                {
                    if (String.Length > i + start + 1 && String[i + start + 1] == '`')
                    {
                        i++;
                        len += 1;
                    }
                    else if (String.Length > i + start + 1 && String[i + start + 1] == '`')
                    {
                        i++;
                        len += 2;
                        continue;
                    }
                    else if (String.Length > i + start + 3)
                    {
                        i += 1;
                        len += 4;
                        continue;
                    }
                }
                Rectangle rgn;
                if (Characters.TryGetValue(String[i], out rgn))
                {
                    nW += rgn.Width;
                    nH = (int)MathHelper.Max(nH, rgn.Height);
                }

                pos.X = (int)MathHelper.Max(pos.X, nW);
            }
            pos.Y += nH + Spacing.Y;
            return pos;
        }

        /// <summary>
        /// Get the start position, ignoring special chars
        /// </summary>
        /// <param name="String">The string to check</param>
        /// <param name="StartIndex">The suppposed start</param>
        /// <returns>The actual start position</returns>
        protected int GetStart(string String, int StartIndex)
        {
            int start = StartIndex < 0 ? 0 : StartIndex;
            for (int i = 0; i < StartIndex; i++) //calculate for escape chars
            {
                if (String[i] == '\n')
                    start++;
                else if (String[i] == '`')
                {
                    if (String.Length > i + 1 && String[i + 1] == '`')
                        start += 1;
                    else if (String.Length > i + 1 && String[i + 1] == 'x')
                    {
                        start += 2;
                        i++;
                    }
                    else if (String.Length > i + 3)
                    {
                        start += 4;
                        i += 3;
                    }
                }
            }
            return start;
        }

        #endregion
    }
}
