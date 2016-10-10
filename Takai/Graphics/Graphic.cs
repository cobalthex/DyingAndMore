using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Graphics
{
    /// <summary>
    /// The style of tweening to apply to the image
    /// </summary>
    public enum TweenStyle
    {
        /// <summary>
        /// No tweening is applied
        /// </summary>
        None,
        /// <summary>
        /// The previous frame is faded out while the next frame is faded in
        /// </summary>
        Overlap,
        /// <summary>
        /// The next frame is faded in before the previous frame is faded out
        /// </summary>
        Sequential,
    }

    /// <summary>
    /// A graphic animation (A sprite)
    /// </summary>
    [Data.CustomSerialize(typeof(Graphic), "Serialize"), Data.CustomDeserialize(typeof(Graphic), "Deserialize")]
    public class Graphic : Animation
    {
        /// <summary>
        /// The file that this graphic was loaded from
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The source texture
        /// </summary>
        public Texture2D Texture { get; set; }

        /// <summary>
        /// The width of the graphic (the width of one frame)
        /// </summary>
        public int Width
        {
            get { return width; }
            set
            {
                width = value;
                if (value != 0)
                    framesPerRow = (ClipRect.Width / value);
            }
        }
        private int width;

        /// <summary>
        /// The height of the graphic (the height of one frame)
        /// </summary>
        public int Height
        {
            get { return height; }
            set { height = value; }
        }
        private int height;

        /// <summary>
        /// Width and height represented as a point
        /// </summary>
        public Point Size { get { return new Point(Width, Height); } }

        protected int framesPerRow;

        /// <summary>
        /// The region of the texture to use
        /// </summary>
        /// <remarks>Only full size frames are used, so any leftover space is ignored</remarks>
        public Rectangle ClipRect
        {
            get { return clipRect; }
            set
            {
                clipRect = value;
                if (width != 0)
                    framesPerRow = (value.Width / width);
            }
        }
        private Rectangle clipRect;

        /// <summary>
        /// The origin of rotation for drawing
        /// </summary>
        public Vector2 Origin { get; set; }

        /// <summary>
        /// How inter-frames should be displayed
        /// </summary>
        public TweenStyle Tween { get; set; }

        public Graphic() { }

        public Graphic(Texture2D Texture)
            : base()
        {
            this.Texture = Texture;
            if (Texture != null)
            {
                File = Texture.Name;
                Width = Texture.Width;
                Height = Texture.Height;
                ClipRect = Texture.Bounds;
            }
        }

        public Graphic(Texture2D Texture, Rectangle ClipRect)
            : base()
        {
            this.Texture = Texture;
            this.Width = ClipRect.Width;
            this.Height = ClipRect.Height;
            this.ClipRect = ClipRect;
            this.Origin = Vector2.Zero;
            this.Tween = TweenStyle.None;
        }

        public Graphic
        (
            Texture2D Texture,
            int Width,
            int Height,
            int FrameCount,
            System.TimeSpan FrameTime,
            TweenStyle TweenStyle,
            bool ShouldLoop,
            bool StartImmediately = true
        ) : base(FrameCount, FrameTime, ShouldLoop, StartImmediately)
        {
            this.Texture = Texture;
            this.Width = Width;
            this.Height = Height;
            if (Texture != null)
                this.ClipRect = Texture.Bounds;
            this.Origin = Vector2.Zero;
            this.Tween = TweenStyle;
        }

        public Graphic
        (
            Texture2D Texture,
            int Width,
            int Height,
            Rectangle ClipRect,
            int FrameCount,
            System.TimeSpan FrameTime,
            TweenStyle TweenStyle,
            bool ShouldLoop,
            bool StartImmediately = true
        ) : base(FrameCount, FrameTime, ShouldLoop, StartImmediately)
        {
            this.Texture = Texture;
            this.Width = Width;
            this.Height = Height;
            this.ClipRect = ClipRect;
            this.Origin = Vector2.Zero;
            this.Tween = TweenStyle;
        }

        public Graphic Clone()
        {
            return (Graphic)this.MemberwiseClone();
        }

        /// <summary>
        /// A helper method to center the origin of the image
        /// </summary>
        /// <returns>The centered origin</returns>
        public Vector2 CenterOrigin()
        {
            Origin = new Vector2(width / 2, height / 2);
            return Origin;
        }

        /// <summary>
        /// Get the clip rect for a single frame of the image
        /// </summary>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public Rectangle GetFrameRect(int Frame)
        {
            var src = new Rectangle(ClipRect.X + (Frame % framesPerRow) * Width, ClipRect.Y + (Frame / framesPerRow) * Height, Width, Height);
            return Rectangle.Intersect(src, ClipRect);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle)
        {
            Draw(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), Angle, Color.White);
        }
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle)
        {
            Draw(SpriteBatch, Bounds, Angle, Color.White);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle, Color Color, float Scale = 1)
        {
            Draw(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), Angle, Color);   
        }

        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle, Color Color)
        {
            //todo: bounds should maybe ignore origin

            float frame = CurrentFrameDelta;
            int cf = (int)frame;
            int nf = (IsLooping ? ((cf + 1) % FrameCount) : MathHelper.Clamp(cf + 1, 0, FrameCount));
            float fd = frame - cf;

            switch (Tween)
            {
                case TweenStyle.None:
                    DrawTexture(SpriteBatch, Bounds, GetFrameRect(cf), Angle, Color);
                    break;

                case TweenStyle.Overlap:
                    //todo: overlap should fade in and then fade out (maybe multiply fd * 2 and Clamp)
                    var nd = MathHelper.Clamp(fd * 2, 0, 1);
                    fd = MathHelper.Clamp((fd * 2) - 1, 0, 1);
                    DrawTexture(SpriteBatch, Bounds, GetFrameRect(cf), Angle, Color.Lerp(Color, Color.Transparent, fd));
                    DrawTexture(SpriteBatch, Bounds, GetFrameRect(nf), Angle, Color.Lerp(Color.Transparent, Color, nd));
                    break;

                case TweenStyle.Sequential:
                    DrawTexture(SpriteBatch, Bounds, GetFrameRect(cf), Angle, Color.Lerp(Color, Color.Transparent, fd));
                    DrawTexture(SpriteBatch, Bounds, GetFrameRect(nf), Angle, Color.Lerp(Color.Transparent, Color, fd));
                    break;
            }
        }
        
        /// <summary>
        /// Draw the actual texture of the graphic. Used by Draw()
        /// </summary>
        /// <param name="SpriteBatch">The spritebatch to use</param>
        /// <param name="Bounds">Where to draw the frame</param>
        /// <param name="SourceRect">What part of the graphic to draw</param>
        /// <param name="Angle">The angle to rotate the drawing by (in radians)</param>
        /// <param name="Color">The color to tint the image</param>
        /// <remarks>Does not check if texture is null</remarks>
        public void DrawTexture(SpriteBatch SpriteBatch, Rectangle Bounds, Rectangle SourceRect, float Angle, Color Color)
        {
            SpriteBatch.Draw(Texture, Bounds, SourceRect, Color, Angle, Origin, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Calculate the dest rectangle to fit an image into a rectangle:
        /// Shrink if too large and center if too small to fit into the <see cref="Region"/>
        /// </summary>
        /// <param name="Width">The width of the image</param>
        /// <param name="Height">The height of the image</param>
        /// <param name="Region">The region to fit to</param>
        /// <returns>The fit rectangle</returns>
        public static Rectangle GetFitRect(int Width, int Height, Rectangle Region)
        {
            Width = MathHelper.Min(Width, Region.Width);
            Height = MathHelper.Min(Height, Region.Height);
            return new Rectangle(Region.X + MathHelper.Max(0, (Region.Width - Width) / 2), Region.Y + MathHelper.Max(0, (Region.Height - Height) / 2), Width, Height);
        }
        /// <summary>
        /// Calculate the dest rectangle to contain an image into a rectangle:
        /// Shrink if too large and center if too small to fit into the <see cref="Region"/>
        /// while maintaining the same aspect ratio
        /// </summary>
        /// <param name="Width">The width of the image</param>
        /// <param name="Height">The height of the image</param>
        /// <param name="Region">The region to contain to</param>
        /// <returns>The contain rectangle</returns>
        public static Rectangle GetContainRect(int Width, int Height, Rectangle Region)
        {
            //todo
            Width = MathHelper.Min(Width, Region.Width);
            Height = MathHelper.Min(Height, Region.Height);
            return new Rectangle(Region.X + MathHelper.Max(0, (Region.Width - Width) / 2), Region.Y + MathHelper.Max(0, (Region.Height - Height) / 2), Width, Height);
        }

        [Data.DesignerCreatable]
        struct GraphicSave
        {
            public Texture2D texture;
            public int width;
            public int height;
            public Rectangle clip;
            public Vector2 origin;
            public int frameCount;
            public System.TimeSpan frameTime;
            public bool isLooping;
            public bool isRunning;
            public TweenStyle tween;
            public System.TimeSpan delay;
            public System.TimeSpan elapsed;
        }

        public static object Serialize(Graphic Graphic)
        {
            if (Graphic == null)
                return null;

            if (Graphic.File != null)
                return Graphic.File;

            var elapsed = Graphic.ElapsedMilliseconds;
            if (Graphic.IsLooping)
                elapsed %= (long)Graphic.TotalFrameTime.TotalMilliseconds;

            GraphicSave save;
            save.texture = Graphic.Texture;
            save.width = Graphic.Width;
            save.height = Graphic.Height;
            save.clip = Graphic.ClipRect;
            save.origin = Graphic.Origin;
            save.frameCount = Graphic.FrameCount;
            save.frameTime = Graphic.FrameTime;
            save.isLooping = Graphic.IsLooping;
            save.isRunning = Graphic.IsRunning;
            save.tween = Graphic.Tween;
            save.delay = Graphic.StartDelay;
            save.elapsed = System.TimeSpan.FromMilliseconds(elapsed);
            return save;
        }

        public static object Deserialize(object Intermediate)
        {
            var file = Intermediate as string;
            if (file != null)
                return FromFile(file);
            
            if (Intermediate is GraphicSave)
            {
                var save = (GraphicSave)Intermediate;

                Graphic g = new Graphic();

                //auto calc clip rect
                if (save.texture != null)
                {
                    if (save.clip.Width == 0)
                        save.clip.Width = MathHelper.Min(save.texture.Width - save.clip.X, save.width * save.frameCount);
                    if (save.clip.Height == 0)
                        save.clip.Height = MathHelper.Min(save.texture.Height - save.clip.Y, save.height * save.frameCount);
                }
                else
                {
                    save.clip.Width = MathHelper.Max(save.clip.Width, 1);
                    save.clip.Height = MathHelper.Max(save.clip.Height, 1);
                }

                g.Texture = save.texture;
                g.Width = save.width;
                g.Height = save.height;
                g.ClipRect = save.clip;
                g.Origin = save.origin;
                g.FrameCount = save.frameCount;
                g.FrameTime = save.frameTime;
                g.IsLooping = save.isLooping;
                g.Tween = save.tween;
                g.StartDelay = save.elapsed - save.delay;

                if (save.isRunning)
                    g.Start();

                return g;
            }

            return null;
        }

        /// <summary>
        /// Load a graphic from a file
        /// </summary>
        /// <param name="File">The file to load from</param>
        /// <returns>The graphic loaded, or null if there was an error</returns>
        public static Graphic FromFile(string File)
        {
            if (File.EndsWith(".tk"))
            {
                using (var reader = new System.IO.StreamReader(File))
                {
                    object g;
                    if (Data.Serializer.EvaluateAs(typeof(Graphic), Data.Serializer.TextDeserialize(reader), out g))
                        ((Graphic)g).File = File;
                    return (Graphic)g;
                }
            }

            var tex = Takai.AssetManager.Load<Texture2D>(File);
            return tex != null ? new Graphic(tex) : null;
        }
    }
}
