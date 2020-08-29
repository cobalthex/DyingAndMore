using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Graphics
{

    /// <summary>
    /// How to align characters of a font
    /// </summary>
    public enum VerticalAlignment
    {
        Top,
        Bottom
    }

    /// <summary>
    /// Text styling options to apply to a text render
    /// </summary>
    public struct TextStyle
    {
        public bool monospace;
        public bool underline;
        public bool oblique;

        public float size; //size of the font, (in pixels height), if 0, default is used

        public float outlineThickness; //0-1
        public Color outlineColor;

        //outlined
        //bold
        //strikethrough
    }

    [Data.CustomDeserialize(typeof(BitmapFont), "DeserializeFont")]
    public class Font : Data.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        /// <summary>
        /// All of the available characters in the font
        /// </summary>
        public Dictionary<char, Rectangle> Characters
        {
            get => _characters;
            protected set
            {
                _characters = value;
                MaxCharWidth = 0;
                MaxCharHeight = 0;
                foreach (var @char in _characters)
                {
                    MaxCharWidth = Math.Max(MaxCharWidth, @char.Value.Width);
                    MaxCharHeight = Math.Max(MaxCharHeight, @char.Value.Height);
                }
            }
        }
        private Dictionary<char, Rectangle> _characters;

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

        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// The amount of lateral slant to apply when drawing this font as oblique
        /// Value is a fraction of <see cref="MaxCharWidth"/>
        /// </summary>
        public float ObliqueSlant { get; set; } = 0.1f;
    }
    public struct DrawTextOptions
    {
        public static float DefaultTextSize = 20f;

        public Font font;

        public string text;
        public int textOffset;
        public int textLength;
        public Color color;
        public TextStyle style;
        public float sizeFraction; //size of text, in fraction style.size / font.MaxCharHeight

        //clip container
        public Vector2 position;
        public Vector2 clipSize; //better name
        public Vector2 relativeOffset; //offset inside the container
        public Matrix? transform;

        /// <summary>
        /// Create options to draw text
        /// Options can be further filled with initializer list
        /// </summary>
        /// <param name="font">The font to use when drawing</param>
        /// <param name="text">The text to draw</param>
        /// <param name="color">The color to draw the text</param>
        /// <param name="position">Where to draw the text</param>
        public DrawTextOptions(string text, Font font, Color color, Vector2 position)
            : this(text, font, new TextStyle(), color, position) { }

        public DrawTextOptions(string text, Font font, TextStyle style, Color color, Vector2 position)
        { 
            this.font = font;
            this.text = text;
            textOffset = 0;
            textLength = text.Length; //formatting characters not included in length calculation
            this.style = style;
            sizeFraction = (style.size == 0 ? DefaultTextSize : style.size) / font.MaxCharWidth;
            this.color = color;

            this.position = position;
            clipSize = new Vector2(100000); //todo: use max value
            relativeOffset = Vector2.Zero;
            transform = null;
        }
    }

    public class TextRenderer
    {
        public static TextRenderer Default;

        protected struct TextVertex : IVertexType
        {
            //use half vectors?
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1),
                new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.Position, 1),
            });

            public Vector2 position;
            public Color color;
            public Vector2 texcoord;
            public Color outlineColor;
            public float outlineThickness;

            public TextVertex(Vector2 position, Color color, Vector2 texcoord, Color outlineColor, float outlineThickness)
            {
                this.position = position;
                this.color = color;
                this.texcoord = texcoord;
                this.outlineColor = outlineColor;
                this.outlineThickness = outlineThickness;
            }

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
        }

        protected class RenderBatch //use struct?
        {
            public Matrix transform; //todo: this should be part of hash/key

            public TextVertex[] vertices;
            public int nextVertexIndex;
        }

        protected Dictionary<Texture2D, RenderBatch> batches;

        protected Effect shader; //pass in during draw call?

        /// <summary>
        /// Creata a new text renderer
        /// </summary>
        /// <param name="initialVertexCount">The number of initial vertices to contain. Each character takes 6 vertices</param>
        /// <param name="textShader">The shader to render text with</param>
        public TextRenderer(Effect textShader)
        {
            batches = new Dictionary<Texture2D, RenderBatch>();
            shader = textShader;
        }

        protected void EnsureCapacity(int additionalCapacity, RenderBatch batch)
        {
            if (batch.vertices.Length < batch.nextVertexIndex + additionalCapacity)
            {
                additionalCapacity = Math.Max(batch.vertices.Length * 3 / 2, batch.nextVertexIndex + additionalCapacity);
                var newVertices = new TextVertex[additionalCapacity];
                batch.vertices.CopyTo(newVertices, 0);
                batch.vertices = newVertices;
            }
        }

        private int ReadHex(string text, int offset)
        {
            char ch = text[offset];
            if (ch >= '0' && ch <= '9')
                return (ch - '0');
            if (ch >= 'a' && ch <= 'f')
                return 10 + (ch - 'a');
            if (ch >= 'A' && ch <= 'F')
                return 10 + (ch - 'A');
            return 0;
        }

        /// <summary>
        /// Draw some text
        /// </summary>
        /// <param name="options">The text and optiosn to draw the text with</param>
        /// <returns>The dimensions of the virtual size of the rendered text (not clipped)</returns>
        public Extent Draw(DrawTextOptions options)
        {
            if (!batches.TryGetValue(options.font.Texture, out var batch))
            {
                batch = batches[options.font.Texture] = new RenderBatch
                {
                    vertices = new TextVertex[4800],
                    nextVertexIndex = 0,
                    transform = Matrix.Identity //todo
                };
            }

            Color color = options.color;
            TextStyle style = options.style;

            var texFrac = new Vector2(1f / options.font.Texture.Width, 1f / options.font.Texture.Height);
            var clipFrac = options.clipSize / options.sizeFraction;

            var slantFrac = options.font.ObliqueSlant * options.font.MaxCharWidth;
            float currentSlant = style.oblique ? slantFrac : 0;

            var length = Math.Min(options.text.Length, options.textOffset + options.textLength);
            EnsureCapacity(length, batch);

            //todo: underline may need extra verts

            Vector2 offset = options.relativeOffset;
            float lineHeight = 0; //scaled by size on newline
            float underlineX = 0;

            //todo: italics can push text out of container

            var drawnSize = new Extent(options.position, options.position);
            drawnSize.min.X -= currentSlant;

            //todo: text scale

            //length ignores formatting characters
            for (int i = options.textOffset; i < Math.Min(length, options.text.Length); ++i)
            {
                switch (options.text[i])
                {
                    case '\n':
                        {
                            //handle underlines
                            offset.X = options.relativeOffset.X;
                            offset.Y += (lineHeight + options.font.Tracking.Y);
                            lineHeight = 0;
                            underlineX = 0;
                            break;
                        }

                    case '`':
                        {
                            if (i + 1 >= options.text.Length)
                                break;

                            switch (options.text[i + 1])
                            {
                                //`x - reset style + color
                                case 'x':
                                    //handle underline
                                    color = options.color;
                                    style = options.style;
                                    currentSlant = style.oblique ? slantFrac : 0;
                                    length += 2;
                                    ++i;
                                    continue;

                                //`_ - toggle underline
                                case '_':
                                    style.underline ^= true;
                                    underlineX = offset.X;
                                    length += 2;
                                    ++i;
                                    continue;

                                //`/ - toggle italics
                                case '/':
                                    style.oblique ^= true;
                                    currentSlant = style.oblique ? slantFrac : 0;
                                    length += 2;
                                    ++i;
                                    continue;

                                //`c[rgb] - change color
                                case 'c':
                                    if (i + 4 >= options.text.Length)
                                        break;

                                    color = new Color(
                                        ReadHex(options.text, i + 2) << 4,
                                        ReadHex(options.text, i + 3) << 4,
                                        ReadHex(options.text, i + 4) << 4,
                                        options.color.A
                                    ); //all -1 to reset?
                                    length += 5;
                                    i += 4;
                                    continue;

                                //`k - reset color
                                case 'k':
                                    color = options.color;
                                    length += 2;
                                    ++i;
                                    continue;

                            }
                            goto default;
                        }

                    default:
                        {
                            if (offset.Y > clipFrac.Y)
                            {
                                drawnSize.max.X += currentSlant;
                                return drawnSize;
                            }
                            if (offset.X >= clipFrac.X)
                                continue; //might be newline

                            if (!options.font.Characters.TryGetValue(options.text[i], out var rgn))
                                continue;

                            //restrict source region to visible area of clip region
                            var rgnExtent = new Extent(rgn);
                            var loc = rgnExtent.min - offset;
                            var clip = Extent.Intersect(rgnExtent, new Extent(loc, loc + clipFrac));

                            if (clip.Size != Vector2.Zero)
                            {
                                Vector2 vloc = options.position + (offset + (clip.min - rgnExtent.min)) * options.sizeFraction;
                                //vloc.Y += (options.font.MaxCharHeight - rgn.Height) * options.sizeFraction; //crude -- should use alignment code
                                Vector2 vsz = clip.Size * options.sizeFraction;

                                var tl = new TextVertex(
                                    new Vector2(vloc.X + currentSlant, vloc.Y),
                                    color, 
                                    new Vector2(clip.min.X, clip.min.Y) * texFrac,
                                    style.outlineColor,
                                    style.outlineThickness
                                );
                                var tr = new TextVertex(
                                    new Vector2(vloc.X + currentSlant + vsz.X, vloc.Y),
                                    color,
                                    new Vector2(clip.max.X, clip.min.Y) * texFrac,
                                    style.outlineColor,
                                    style.outlineThickness
                                );
                                var bl = new TextVertex(
                                    new Vector2(vloc.X - currentSlant, vloc.Y + vsz.Y),
                                    color,
                                    new Vector2(clip.min.X, clip.max.Y) * texFrac,
                                    style.outlineColor,
                                    style.outlineThickness
                                );
                                var br = new TextVertex(
                                    new Vector2(vloc.X - currentSlant, vloc.Y) + vsz,
                                    color,
                                    new Vector2(clip.max.X, clip.max.Y) * texFrac,
                                    style.outlineColor,
                                    style.outlineThickness
                                );

                                batch.vertices[batch.nextVertexIndex++] = tl;
                                batch.vertices[batch.nextVertexIndex++] = tr;
                                batch.vertices[batch.nextVertexIndex++] = bl;

                                batch.vertices[batch.nextVertexIndex++] = bl;
                                batch.vertices[batch.nextVertexIndex++] = tr;
                                batch.vertices[batch.nextVertexIndex++] = br;
                            }

                            offset.X += ((style.monospace ? options.font.MaxCharWidth : rgn.Width) + options.font.Tracking.X);
                            lineHeight = Math.Max(lineHeight, rgn.Height); //include underline height

                            break;
                        }
                }
            }

            //draw trailing underline

            offset.Y += lineHeight;
            drawnSize.max += offset * options.sizeFraction;
            drawnSize.max.X += -options.font.Tracking.X + currentSlant;
            return drawnSize;
        }

        SpriteEffect testFx;

        //necessary?
        public void Present(GraphicsDevice graphicsDevice, Matrix transform)
        {
            if (testFx == null)
                testFx = new SpriteEffect(graphicsDevice);

            foreach (var batch in batches)
            {
                if (batch.Value.nextVertexIndex == 0)
                    continue;

                shader.Parameters["Transform"].SetValue(transform * batch.Value.transform);
                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    graphicsDevice.Textures[0] = batch.Key;
                    //use (dynamic) vertex buffers?
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.TriangleList,
                        batch.Value.vertices,
                        0, batch.Value.nextVertexIndex / 3
                    );
                }
                batch.Value.nextVertexIndex = 0;
            }
        }

        //public static (Vector2 pos, Vector2 size) Intersect((Vector2 pos, Vector2 size) a, (Vector2 pos, Vector2 size) b)
        //{
        //    var xMax = Math.Max(a.pos.X, b.pos.X);
        //    var xMin = Math.Min(a.pos.X + a.size.X, b.pos.X + b.size.X);
        //    var yMax = Math.Max(a.pos.Y, b.pos.Y);
        //    var yMin = Math.Min(a.pos.Y + a.size.Y, b.pos.Y + b.size.Y);
        //    if (xMin >= xMax && yMin >= yMax)
        //        return (new Vector2(xMax, yMax), new Vector2(xMin - xMax, yMin - yMax));
        //    else
        //        return (Vector2.Zero, Vector2.Zero);
        //}
    }
}
