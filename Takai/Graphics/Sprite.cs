using System;
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
    /// A graphic animation. Typically synchronized with a map's timer
    /// </summary>
    [Data.DesignerCreatable]
    public class Sprite : ICloneable
    {
        /// <summary>
        /// The elapsed time of this graphic (used for calculating current frame)
        /// Set this value to update the animation
        /// </summary>
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The number of frames of this graphic
        /// </summary>
        public int FrameCount { get; set; } = 1;

        /// <summary>
        /// The length of time of each frame
        /// </summary>
        public TimeSpan FrameLength { get; set; } = TimeSpan.FromTicks(1);

        /// <summary>
        /// Is the graphic looping?
        /// </summary>
        public bool IsLooping { get; set; } = false;

        /// <summary>
        /// The current frame
        /// </summary>
        [Data.NonSerialized]
        public int CurrentFrame
        {
            get
            {
                var frame = (int)(ElapsedTime.TotalSeconds / FrameLength.TotalSeconds);
                return IsLooping ? (frame % FrameCount) : MathHelper.Clamp(frame, 0, FrameCount - 1);
            }
        }

        /// <summary>
        /// THe next frame
        /// </summary>
        [Data.NonSerialized]
        public int NextFrame
        {
            get
            {
                var frame = (int)(ElapsedTime.TotalSeconds / FrameLength.TotalSeconds) + 1;
                return IsLooping ? (frame % FrameCount) : MathHelper.Clamp(frame, 0, FrameCount - 1);
            }
        }

        /// <summary>
        /// The fractional amount between the current and next frame
        /// </summary>
        [Data.NonSerialized]
        public float FrameDelta
        {
            get
            {
                var frame = ElapsedTime.TotalSeconds / FrameLength.TotalSeconds;
                return (float)frame - (int)frame;
            }
        }

        /// <summary>
        /// The file that this graphic was loaded from
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The source texture
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return texture;
            }
            set
            {
                texture = value;
                if (texture != null && clipRect == Rectangle.Empty)
                    ClipRect = texture.Bounds;
            }
        }

        private Texture2D texture;

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
        [Data.NonSerialized]
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
        public Vector2 Origin { get; set; } = Vector2.Zero;

        /// <summary>
        /// How inter-frames should be displayed
        /// </summary>
        public TweenStyle Tween { get; set; } = TweenStyle.None;

        public Sprite() { }

        public Sprite(Texture2D Texture)
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

        public Sprite(Texture2D Texture, Rectangle ClipRect)
            : base()
        {
            this.Texture = Texture;
            this.Width = ClipRect.Width;
            this.Height = ClipRect.Height;
            this.ClipRect = ClipRect;
        }

        public Sprite
        (
            Texture2D Texture,
            int Width,
            int Height,
            int FrameCount,
            TimeSpan FrameLength,
            TweenStyle TweenStyle,
            bool ShouldLoop)
        {
            this.Texture = Texture;
            this.Width = Width;
            this.Height = Height;
            if (Texture != null)
                this.ClipRect = Texture.Bounds;
            this.FrameCount = FrameCount;
            this.FrameLength = FrameLength;
            this.Tween = TweenStyle;
            this.IsLooping = ShouldLoop;
        }

        public Sprite
        (
            Texture2D Texture,
            int Width,
            int Height,
            Rectangle ClipRect,
            int FrameCount,
            TimeSpan FrameLength,
            TweenStyle TweenStyle,
            bool ShouldLoop)
        {
            this.Texture = Texture;
            this.Width = Width;
            this.Height = Height;
            this.ClipRect = ClipRect;
            this.FrameCount = FrameCount;
            this.FrameLength = FrameLength;
            this.Tween = TweenStyle;
            this.IsLooping = ShouldLoop;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Is the animation finished playing?
        /// </summary>
        /// <returns></returns>
        public bool HasFinished()
        {
            return !IsLooping && ElapsedTime >= TimeSpan.FromTicks(FrameLength.Ticks * FrameCount);
        }


        /// <summary>
        /// (Re)start the animation
        /// </summary>
        /// <param name="Time">The time to start counting from</param>
        public void Start()
        {
            ElapsedTime = TimeSpan.Zero;
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
        /// <param name="Frame">Which frame of the animation to use (No bounds checking)</param>
        /// <returns>The clipping rectangle of the requested frame</returns>
        public Rectangle GetFrameRect(int Frame)
        {
            var src = new Rectangle(ClipRect.X + (Frame % framesPerRow) * Width, ClipRect.Y + (Frame / framesPerRow) * Height, Width, Height);
            return Rectangle.Intersect(src, ClipRect);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle)
        {
            Draw(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), Angle, Color.White, ElapsedTime);
        }
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle)
        {
            Draw(SpriteBatch, Bounds, Angle, Color.White, ElapsedTime);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle, Color Color, float Scale = 1)
        {
            Draw(SpriteBatch, new Rectangle((int)Position.X, (int)Position.Y, width, height), Angle, Color, ElapsedTime);
        }

        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle, Color Color, TimeSpan  ElapsedTime)
        {
            var elapsed = (float)(ElapsedTime.TotalSeconds / FrameLength.TotalSeconds);
            var cf = IsLooping ? ((int)elapsed % FrameCount) : MathHelper.Clamp((int)elapsed, 0, FrameCount - 1);
            var nf = IsLooping ? ((cf + 1) % FrameCount) : MathHelper.Clamp(cf + 1, 0, FrameCount - 1);
            var fd = elapsed % 1;

            //todo: bounds should maybe ignore origin

            switch (Tween)
            {
                case TweenStyle.None:
                    DrawTexture(SpriteBatch, Bounds, GetFrameRect(cf), Angle, Color);
                    break;

                case TweenStyle.Overlap:
                    //todo: verify works
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
    }
}
