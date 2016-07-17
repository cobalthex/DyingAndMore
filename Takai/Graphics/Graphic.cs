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
            File = Texture.Name;
            Width = Texture.Width;
            Height = Texture.Height;
            ClipRect = Texture.Bounds;
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
            Draw(SpriteBatch, Position, Angle, Color.White);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle, Color Color)
        {
            float frame = CurrentFrameDelta;
            int cf = (int)frame;
            int nf = (IsLooping ? ((cf + 1) % FrameCount) : MathHelper.Clamp(cf + 1, 0, FrameCount));
            float fd = frame - cf;

            switch (Tween)
            {
                case TweenStyle.None:
                    DrawFrame(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), GetFrameRect(cf), Angle, Color);
                    break;

                case TweenStyle.Overlap:
                    //todo: overlap should fade in and then fade out (maybe multiply fd * 2 and Clamp)
                    var nd = MathHelper.Clamp(fd * 2, 0, 1);
                    fd = MathHelper.Clamp((fd * 2) - 1, 0, 1);
                    DrawFrame(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), GetFrameRect(cf), Angle, Color.Lerp(Color, Color.Transparent, fd));
                    DrawFrame(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), GetFrameRect(nf), Angle, Color.Lerp(Color.Transparent, Color, nd));
                    break;

                case TweenStyle.Sequential:
                    DrawFrame(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), GetFrameRect(cf), Angle, Color.Lerp(Color, Color.Transparent, fd));
                    DrawFrame(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), GetFrameRect(nf), Angle, Color.Lerp(Color.Transparent, Color, fd));
                    break;
            }
        }

        public void DrawFrame(SpriteBatch SpriteBatch, Rectangle Bounds, Rectangle SourceRect, float Angle, Color Color)
        {
            SpriteBatch.Draw(Texture, Bounds, SourceRect, Color, Angle, Origin, SpriteEffects.None, 0);
        }

        public static object Serialize(Graphic Graphic)
        {
            if (Graphic == null)
                return null;

            if (Graphic.File != null)
                return Graphic.File;
            
            return new Dictionary<string, object>
            {
                { "texture", Graphic.Texture },
                { "width", Graphic.width },
                { "height", Graphic.height },
                { "clipRect", Graphic.clipRect },
                { "origin", Graphic.Origin },
                { "frameCount", Graphic.FrameCount },
                { "frameTime", Graphic.FrameTime },
                { "isLooping", Graphic.IsLooping },
                { "isRunning", Graphic.IsRunning },
                { "tween", Graphic.Tween },
                { "offset", Graphic.TimeOffset },
                { "elapsed", Graphic.ElapsedMilliseconds }
            };
        }

        public static object Deserialize(object Intermediate)
        {
            var file = Intermediate as string;
            if (file != null)
                return FromFile(file);

            var dict = Intermediate as Dictionary<string, object>;
            if (dict != null)
            {
                Graphic g = new Graphic();
                g.Texture = Takai.AssetManager.Load<Texture2D>(dict["texture"] as string); //todo: use texture Deserializer
                g.Width =  (int)dict["width"];
                g.Height = (int)dict["height"];

                Rectangle clipRect;
                Data.Serializer.DeserializeAs(dict["clipRect"], out clipRect);
                g.ClipRect = clipRect;

                Vector2 origin;
                Data.Serializer.DeserializeAs(dict["origin"], out origin);
                g.Origin = origin;

                g.Tween = (TweenStyle)dict["tween"];
                g.FrameCount = (int)dict["frameCount"];

                System.TimeSpan frameTime, elapsed, offset;
                Data.Serializer.DeserializeAs(dict["frameTime"], out frameTime);
                Data.Serializer.DeserializeAs(dict["elapsed"], out elapsed);
                Data.Serializer.DeserializeAs(dict["offset"], out offset);

                g.FrameTime = frameTime;
                g.TimeOffset = elapsed - offset;

                g.IsLooping = (bool)dict["isLooping"];

                if ((bool)dict["isRunning"])
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
                    Graphic g;
                    Data.Serializer.DeserializeAs(Data.Serializer.TextDeserialize(reader), out g);
                    g.File = File;
                    return g;
                }
            }

            var tex = Takai.AssetManager.Load<Texture2D>(File);
            return tex != null ? new Graphic(tex) : null;
        }
    }
}
