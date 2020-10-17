using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Graphics
{
    /// <summary>
    /// Text styling options to apply to a text render
    /// </summary>
    public struct TextStyle
    {
        public static float DefaultTextSize = 20f;

        public bool monospace;
        public bool underline;
        public bool oblique;
        public bool ignoreFormattingCharacters;

        public float size; //size of the font, (in pixels height), if 0, default is used

        public float outlineThickness; //0-1
        public Color outlineColor;

        //bold
        //strikethrough

        public static bool operator ==(TextStyle a, TextStyle b) => a.Equals(b);
        public static bool operator !=(TextStyle a, TextStyle b) => !a.Equals(b);
        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => base.Equals(obj);
    }

    public struct CharacterRegion
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public int xOffset; //probably not necessary
        public int yOffset;
        public int xAdvance;
    }

    public class Font : Data.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        /// <summary>
        /// All of the available characters in the font
        /// </summary>
        public Dictionary<char, CharacterRegion> Characters
        {
            get
            {
                return this._characters;
            }
            protected set
            {
                this._characters = value;
                this.maxAdvance = 0;
                this.maxCharHeight = 0;
                foreach (KeyValuePair<char, CharacterRegion> @char in this._characters)
                {
                    this.maxAdvance = Math.Max(this.maxAdvance, @char.Value.xAdvance);
                    this.maxCharHeight = Math.Max(this.maxCharHeight, @char.Value.height);
                }
            }
        }
        private Dictionary<char, CharacterRegion> _characters;

        /// <summary>
        /// The source texture holding all of the character images
        /// </summary>
        public Texture2D Texture { get; protected set; }

        /// <summary>
        /// The maximum x-advance of all characters
        /// </summary>
        [Data.Serializer.IgnoredAttribute]
        protected internal int maxAdvance;

        /// <summary>
        /// The maximum char height of each character
        /// </summary>
        [Data.Serializer.IgnoredAttribute]
        protected internal int maxCharHeight;

        /// <summary>
        /// The amount of lateral slant to apply when drawing this font as oblique
        /// Value is a fraction of <see cref="MaxCharWidth"/>
        /// </summary>
        public float ObliqueSlant { get; set; } = 0.05f;

        public float GetLineHeight(TextStyle style) => GetLineHeight(style.size);

        public float GetLineHeight(float size) => maxCharHeight * GetScale(size);

        internal float GetScale(float size) => ((size == 0f) ? TextStyle.DefaultTextSize : size) / maxAdvance;

        public Vector2 MeasureString(string text, TextStyle style, int textOffset = 0, int textLength = -1) =>
            MeasureString(text, style.size == 0 ? TextStyle.DefaultTextSize : style.size, style, textOffset, textLength);
        
        public Vector2 MeasureString(string text, float size, TextStyle style, int textOffset = 0, int textLength = -1)
        {
            if (text == null)
                return Vector2.Zero;

            if (textLength < 0)
                textLength = text.Length;

            float lastXUnderhang = 0;
            Vector2 total = Vector2.Zero;
            Vector2 row = Vector2.Zero;
            TextStyle curStyle = style;
            curStyle.size = size;

            float scale = GetScale(size);

            for (int i = textOffset; i < Math.Min(textLength, text.Length); ++i)
            {
                switch (text[i])
                {
                    case '\n':
                        if (curStyle.underline)
                            row.Y = Math.Max(row.Y, (maxCharHeight * scale) + 4
                                );
                        total.X = Math.Max(total.X, row.X) + lastXUnderhang;
                        total.Y += row.Y;
                        row = Vector2.Zero;
                        break;

                    case '`':
                        if (i + 1 >= text.Length || style.ignoreFormattingCharacters)
                            goto default;

                        switch (text[i + 1])
                        {
                            //`x - reset style + color
                            case 'x':
                                if (curStyle.underline)
                                    row.Y = Math.Max(row.Y, (maxCharHeight * scale) + 4);
                                curStyle = style;
                                textLength += 2;
                                ++i;
                                continue;

                            //`+ - increase text size by 20%
                            case '+':
                                curStyle.size *= 1.2f;
                                scale *= 1.2f;
                                textLength += 2;
                                ++i;
                                continue;

                            //`+ - decrease text size by 20%
                            case '-':
                                curStyle.size *= 0.8f;
                                scale *= 0.8f;
                                textLength += 2;
                                ++i;
                                continue;

                            //`_ - toggle underline
                            case '_':
                                if (curStyle.underline)
                                    row.Y = Math.Max(row.Y, (maxCharHeight * scale) + 4);
                                curStyle.underline ^= true;
                                textLength += 2;
                                ++i;
                                continue;

                            //`/ - toggle italics
                            case '/':
                                curStyle.oblique ^= true;
                                textLength += 2;
                                ++i;
                                continue;

                            //`c[rgb] - change color
                            case 'c':
                                if (i + 4 >= text.Length)
                                    break;

                                textLength += 5;
                                i += 4;
                                continue;

                            //`k - reset color
                            case 'k':
                                textLength += 2;
                                ++i;
                                continue;

                        }
                        goto default;

                    default:
                        if (Characters.TryGetValue(text[i], out var rgn))
                        {
                            if (style.monospace)
                            {
                                lastXUnderhang = 0;
                                row.X += maxAdvance * scale;
                            }
                            else
                            {
                                lastXUnderhang = (rgn.width - rgn.xAdvance) * scale;
                                row.X += rgn.xAdvance * scale;
                            }
                            row.Y = Math.Max(row.Y, (rgn.height + rgn.yOffset) * scale);
                        }
                        break;
                }
            }

            if (curStyle.underline)
                row.Y = Math.Max(row.Y, (maxCharHeight * scale) + 4);

            //todo: handle obliques
            total.X = Math.Max(total.X, row.X) + lastXUnderhang;
            total.Y += row.Y;
            return total;
        }
    }

    public struct DrawTextOptions
    {
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
            this.textOffset = 0;
            this.textLength = -1; //formatting characters not included in length calculation
            this.style = style;
            if (this.style.size == 0)
                this.style.size = TextStyle.DefaultTextSize;
            sizeFraction = font?.GetScale(this.style.size) ?? 1;
            this.color = color;

            this.position = position;
            this.clipSize = new Vector2(100000); //todo: use max value
            this.relativeOffset = Vector2.Zero;
            this.transform = null;
        }
    }

    public class TextRenderer
    {
        public static TextRenderer Default;
        public static int MaxBatchIndexCount = short.MaxValue; //round-robins past this number
        public static int MaxBatchVertexCount = MaxBatchIndexCount / 6 * 4; //round-robins past this number

        protected struct TextVertex : IVertexType
        {
            //use half vectors?
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1),
                new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
            });
            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

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
        }

        protected struct TextInstance : IVertexType
        {
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 1),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 2),
            });
            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            public Vector2 offset;
            public Color color;

            public TextInstance(Vector2 offset, Color color)
            {
                this.offset = offset;
                this.color = color;
            }
        }

        protected class RenderBatch //use struct?
        {
            public Matrix transform; //todo: this should be part of hash/key

            public TextVertex[] vertices;
            public short[] indices;
            public short nextVertexIndex;
            public short nextIndexIndex;

            public RenderBatch()
            {
                transform = Matrix.Identity;

                const short defaultTriCount = 1000;
                vertices = new TextVertex[defaultTriCount * 4];
                indices = new short[defaultTriCount * 6];

                nextVertexIndex = nextIndexIndex = 0;
            }
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

        public TextRenderer(TextRenderer copy) : this(copy.shader) { }

        /// <summary>
        /// Resets batches without drawing
        /// </summary>
        public void ResetBatches()
        {
            foreach (var batch in batches)
                batch.Value.nextVertexIndex = 0;
        }

        protected void EnsureCapacity(int additionalTris, RenderBatch batch)
        {
            var additionalVerts = additionalTris * 4;
            var newCount = batch.nextVertexIndex + additionalVerts;
            if (newCount > MaxBatchVertexCount)
            {
                System.Diagnostics.Debug.WriteLine("Ran out of text veritces, looping around");
                batch.nextVertexIndex = 0;
                newCount = additionalVerts;
            }
            if (batch.vertices.Length < newCount)
            {
                newCount = Math.Max(batch.vertices.Length * 3 / 2, newCount);
                var newVertices = new TextVertex[newCount];
                batch.vertices.CopyTo(newVertices, 0);
                batch.vertices = newVertices;
            }

            var additionalIndices = additionalTris * 6;
            newCount = batch.nextIndexIndex + additionalIndices;
            if (newCount > MaxBatchVertexCount)
            {
                System.Diagnostics.Debug.WriteLine("Ran out of text indices, looping around");
                batch.nextIndexIndex = 0;
                newCount = additionalIndices;
            }
            if (batch.indices.Length < newCount)
            {
                newCount = Math.Max(batch.indices.Length * 3 / 2, newCount);
                var newIndices = new short[newCount];
                batch.indices.CopyTo(newIndices, 0);
                batch.indices = newIndices;
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
        public void Draw(DrawTextOptions options)
        {
            if (options.text == null)
                return;

            if (!batches.TryGetValue(options.font.Texture, out var batch))
                batch = batches[options.font.Texture] = new RenderBatch();

            if (options.textLength < 0)
                options.textLength = options.text.Length;

            var length = Math.Min(options.text.Length, options.textOffset + options.textLength);
            EnsureCapacity(length * 2, batch);

            Color color = options.color;
            TextStyle style = options.style;

            var texFrac = new Vector2(1f / options.font.Texture.Width, 1f / options.font.Texture.Height);
            var clipFrac = options.clipSize / options.sizeFraction;

            var slantFrac = options.font.ObliqueSlant * options.font.maxAdvance;
            float currentSlant = style.oblique ? slantFrac : 0;

            //todo: underline may need extra verts

            Vector2 offset = options.relativeOffset / options.sizeFraction;
            float lineHeight = 0; //scaled by size on newline

            float underlineStart = options.relativeOffset.X;

            Action MakeUnderline = () =>
            {
                var xt = Extent.FromRect(
                    options.position + new Vector2(underlineStart, offset.Y + options.font.maxCharHeight + 2) * options.sizeFraction,
                    new Vector2((offset.X - underlineStart) * options.sizeFraction, 2)
                );
                xt = Extent.Intersect(xt, Extent.FromRect(options.position, options.clipSize));

                //todo: clip to bounds

                var tl = new TextVertex(
                    new Vector2(xt.min.X + currentSlant, xt.min.Y),
                    color,
                    new Vector2(-2, -2),
                    style.outlineColor,
                    style.outlineThickness
                );
                var tr = new TextVertex(
                    new Vector2(xt.max.X + currentSlant, xt.min.Y),
                    color,
                    new Vector2(-1, -2),
                    style.outlineColor,
                    style.outlineThickness
                );
                var bl = new TextVertex(
                    new Vector2(xt.min.X - currentSlant, xt.max.Y),
                    color,
                    new Vector2(-2, -1),
                    style.outlineColor,
                    style.outlineThickness
                );
                var br = new TextVertex(
                    new Vector2(xt.max.X - currentSlant, xt.max.Y),
                    color,
                    new Vector2(-1, -1),
                    style.outlineColor,
                    style.outlineThickness
                );

                EnsureCapacity(2, batch);

                batch.indices[batch.nextIndexIndex++] = batch.nextVertexIndex;
                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 1);
                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 2);
                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 2);
                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 1);
                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 3);

                batch.vertices[batch.nextVertexIndex++] = tl;
                batch.vertices[batch.nextVertexIndex++] = tr;
                batch.vertices[batch.nextVertexIndex++] = bl;
                batch.vertices[batch.nextVertexIndex++] = br;
            };

            //todo: italics can push text out of container

            //todo: create initial underline

            //length ignores formatting characters
            for (int i = options.textOffset; i < Math.Min(length, options.text.Length); ++i)
            {
                switch (options.text[i])
                {
                    case '\n':
                        {
                            offset.X = options.relativeOffset.X / options.sizeFraction;
                            offset.Y += lineHeight;
                            lineHeight = 0;

                            if (style.underline)
                            { 
                                MakeUnderline();
                                underlineStart = options.relativeOffset.X;
                            }
                            break;
                        }

                    case '`':
                        {
                            if (i + 1 >= options.text.Length || style.ignoreFormattingCharacters)
                                goto default;

                            switch (options.text[i + 1])
                            {
                                //`x - reset style + color
                                case 'x':
                                    MakeUnderline();
                                    color = options.color;
                                    style = options.style;
                                    currentSlant = style.oblique ? slantFrac : 0;
                                    options.sizeFraction = options.font.GetScale(style.size);
                                    clipFrac = options.clipSize / options.sizeFraction;
                                    length += 2;
                                    ++i;
                                    continue;

                                //`+ - increase text size by 20%
                                case '+':
                                    style.size *= 1.2f;
                                    options.sizeFraction *= 1.2f;
                                    clipFrac = options.clipSize / options.sizeFraction;
                                    length += 2;
                                    ++i;
                                    continue;

                                //`+ - decrease text size by 20%
                                case '-':
                                    style.size *= 0.8f;
                                    options.sizeFraction *= 0.8f;
                                    clipFrac = options.clipSize / options.sizeFraction;
                                    length += 2;
                                    ++i;
                                    continue;

                                //`_ - toggle underline
                                case '_':
                                    if (style.underline)
                                        MakeUnderline();
                                    else
                                        underlineStart = offset.X;

                                    style.underline ^= true;
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
                                return;

                            if (offset.X >= clipFrac.X)
                                continue; //might be newline later in string

                            if (!options.font.Characters.TryGetValue(options.text[i], out var rgn))
                                continue;

                            //restrict source region to visible area of clip region
                            var rgnExtent = new Extent(rgn.x, rgn.y, rgn.x + rgn.width, rgn.y + rgn.height);
                            var drawOffset = new Vector2(rgn.xOffset, rgn.yOffset);
                            var loc = rgnExtent.min - offset - drawOffset;
                            var clip = Extent.Intersect(rgnExtent, new Extent(loc, loc + clipFrac));

                            if (clip.Size != Vector2.Zero)
                            {
                                Vector2 vloc = options.position + (offset + (clip.min - rgnExtent.min) + drawOffset) * options.sizeFraction;
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

                                batch.indices[batch.nextIndexIndex++] = batch.nextVertexIndex;
                                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 1);
                                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 2);
                                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 2);
                                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 1);
                                batch.indices[batch.nextIndexIndex++] = (short)(batch.nextVertexIndex + 3);

                                batch.vertices[batch.nextVertexIndex++] = tl;
                                batch.vertices[batch.nextVertexIndex++] = tr;
                                batch.vertices[batch.nextVertexIndex++] = bl;
                                batch.vertices[batch.nextVertexIndex++] = br;
                            }

                            offset.X += (style.monospace ? options.font.maxAdvance : rgn.xAdvance);
                            lineHeight = Math.Max(lineHeight, rgn.height); //include underline height
                            break;
                        }
                }
            }

            if (style.underline)
                MakeUnderline();
        }

        DynamicVertexBuffer textVertices;
        DynamicIndexBuffer textIndices;
        VertexBuffer textInstances;

        //necessary?
        public void Present(GraphicsDevice graphicsDevice, Matrix transform)
        {
            if (textVertices == null)
            {
                textVertices = new DynamicVertexBuffer(graphicsDevice, typeof(TextVertex), MaxBatchVertexCount, BufferUsage.WriteOnly);
                textIndices = new DynamicIndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, MaxBatchIndexCount, BufferUsage.WriteOnly);

                textInstances = new VertexBuffer(graphicsDevice, typeof(TextInstance), 2, BufferUsage.WriteOnly);
                textInstances.SetData(new[] {
                    new TextInstance(new Vector2(2), Color.Transparent),
                    new TextInstance(new Vector2(0), Color.White),
                });
            }

            foreach (var batch in batches)
            {
                if (batch.Value.nextVertexIndex == 0)
                    continue;

                shader.Parameters["Transform"].SetValue(transform * batch.Value.transform);
                textVertices.SetData(batch.Value.vertices, 0, batch.Value.nextVertexIndex);
                textIndices.SetData(batch.Value.indices, 0, batch.Value.nextIndexIndex);
                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.Textures[0] = batch.Key;
                    graphicsDevice.SetVertexBuffers(
                        new VertexBufferBinding(textVertices),
                        new VertexBufferBinding(textInstances, 0, 1) //option to emboss?
                    );
                    graphicsDevice.Indices = textIndices;
                    //graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, batch.Value.nextVertexIndex / 3);
                    graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, batch.Value.nextIndexIndex / 3, 2);
                }
                batch.Value.nextVertexIndex = 0;
                batch.Value.nextIndexIndex = 0;
            }
        }
    }
}
