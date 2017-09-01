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
    [Data.DesignerModdable]
    [Data.DerivedTypeDeserialize(typeof(Sprite), "DerivedDeserialize")]
    public class Sprite : ICloneable, Data.ISerializeExternally
    {
        /// <summary>
        /// The file that this sprite was loaded from
        /// </summary>
        [Data.Serializer.Ignored]
        public string File { get; set; }

        /// <summary>
        /// The elapsed time of this sprite (used for calculating current frame)
        /// Set this value to update the animation
        /// </summary>
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The number of frames of this sprite
        /// </summary>
        public int FrameCount { get; set; } = 1;

        /// <summary>
        /// The length of time of each frame
        /// </summary>
        public TimeSpan FrameLength { get; set; } = TimeSpan.FromTicks(1);

        /// <summary>
        /// The total length of this animation (FrameCount * FrameLength)
        /// </summary>
        public TimeSpan TotalLength => TimeSpan.FromTicks(FrameLength.Ticks * FrameCount);

        /// <summary>
        /// Is the graphic looping?
        /// </summary>
        public bool IsLooping { get; set; } = false;

        /// <summary>
        /// The current frame
        /// </summary>
        [Data.Serializer.Ignored]
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
        [Data.Serializer.Ignored]
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
        [Data.Serializer.Ignored]
        public float FrameDelta
        {
            get
            {
                var frame = ElapsedTime.TotalSeconds / FrameLength.TotalSeconds;
                return (float)frame - (int)frame;
            }
        }

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
                if (texture != null && ClipRect == Rectangle.Empty)
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
                framesPerRow = MathHelper.Max(1, ClipRect.Width / width);
            }
        }
        private int width = 1;

        /// <summary>
        /// The height of the graphic (the height of one frame)
        /// </summary>
        public int Height
        {
            get { return height; }
            set { height = value; }
        }
        private int height = 1;

        /// <summary>
        /// Width and height represented as a point
        /// </summary>
        [Data.Serializer.ReadOnly]
        public Point Size
        {
            get => new Point(Width, Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        protected int framesPerRow = 1;

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
                framesPerRow = MathHelper.Max(1, value.Width / width);
            }
        }
        private Rectangle clipRect = Rectangle.Empty;

        /// <summary>
        /// The origin of rotation for drawing
        /// </summary>
        public Vector2 Origin { get; set; } = Vector2.Zero;

        /// <summary>
        /// How inter-frames should be displayed
        /// </summary>
        public TweenStyle Tween { get; set; } = TweenStyle.None;

        /// <summary>
        /// When drawing with specified bounds, shrink to fit the bounds, or otherwise center
        /// </summary>
        public bool ShrinkToFit { get; set; } = true;

        public Sprite() { }

        public Sprite(Texture2D texture)
            : base()
        {
            Texture = texture;
            if (texture != null)
            {
                File = texture.Name;
                Width = texture.Width;
                Height = texture.Height;
                ClipRect = texture.Bounds;
            }
        }

        public Sprite(Texture2D texture, Rectangle clipRect)
            : base()
        {
            Texture = texture;
            Width = clipRect.Width;
            Height = clipRect.Height;
            ClipRect = clipRect;
        }

        public Sprite
        (
            Texture2D texture,
            int width,
            int height,
            int frameCount,
            TimeSpan frameLength,
            TweenStyle tweenStyle,
            bool shouldLoop)
        {
            Texture = texture;
            Width = width;
            Height = height;
            if (texture != null)
                ClipRect = texture.Bounds;
            FrameCount = frameCount;
            FrameLength = frameLength;
            Tween = tweenStyle;
            IsLooping = shouldLoop;
        }

        public Sprite
        (
            Texture2D texture,
            int width,
            int height,
            Rectangle clipRect,
            int frameCount,
            TimeSpan frameLength,
            TweenStyle tweenStyle,
            bool shouldLoop)
        {
            Texture = texture;
            Width = width;
            Height = height;
            if (texture != null)
                ClipRect = clipRect;
            FrameCount = frameCount;
            FrameLength = frameLength;
            Tween = tweenStyle;
            IsLooping = shouldLoop;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Is the animation finished playing?
        /// </summary>
        /// <returns>True if the animation isn't looping and has completed</returns>
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
        /// <param name="frame">Which frame of the animation to use (No bounds checking)</param>
        /// <returns>The clipping rectangle of the requested frame</returns>
        public Rectangle GetFrameRect(int frame)
        {
            var src = new Rectangle(ClipRect.X + (frame % framesPerRow) * Width, ClipRect.Y + (frame / framesPerRow) * Height, Width, Height);
            return Rectangle.Intersect(src, ClipRect);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float angle)
        {
            Draw(spriteBatch, new Rectangle(
                (int)position.X - (int)Origin.X,
                (int)position.Y - (int)Origin.Y,
                width,
                height
            ), angle, Color.White, ElapsedTime);
        }
        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, float angle)
        {
            Draw(spriteBatch, bounds, angle, Color.White, ElapsedTime);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float angle, Color color, float scale = 1)
        {
            Draw(spriteBatch, position, angle, color, scale, ElapsedTime);
        }
        public void Draw(SpriteBatch spriteBatch, Vector2 position, float angle, Color color, float scale, TimeSpan elapsedTime)
        {
            var elapsed = (float)(elapsedTime.TotalSeconds / FrameLength.TotalSeconds);
            var cf = IsLooping ? ((int)elapsed % FrameCount) : MathHelper.Clamp((int)elapsed, 0, FrameCount - 1);
            var nf = IsLooping ? ((cf + 1) % FrameCount) : MathHelper.Clamp(cf + 1, 0, FrameCount - 1);
            var fd = elapsed % 1;

            var (curTween, nextTween) = GetTween(fd, Tween);
            spriteBatch.Draw(Texture, position, GetFrameRect(cf), Color.Lerp(color, Color.Transparent, curTween), angle, Origin, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(Texture, position, GetFrameRect(nf), Color.Lerp(color, Color.Transparent, nextTween), angle, Origin, scale, SpriteEffects.None, 0);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, float angle, Color color, TimeSpan elapsedTime)
        {
            var elapsed = (float)(elapsedTime.TotalSeconds / FrameLength.TotalSeconds);
            var cf = IsLooping ? ((int)elapsed % FrameCount) : MathHelper.Clamp((int)elapsed, 0, FrameCount - 1);
            var nf = IsLooping ? ((cf + 1) % FrameCount) : MathHelper.Clamp(cf + 1, 0, FrameCount - 1);
            var fd = elapsed % 1;

            if (ShrinkToFit) //todo: fit/contain
                bounds = GetFitRect(Width, Height, bounds);

            bounds.X += (int)(Origin.X / width * bounds.Width);
            bounds.Y += (int)(Origin.Y / height * bounds.Height);

            var (curTween, nextTween) = GetTween(fd, Tween);
            spriteBatch.Draw(Texture, bounds, GetFrameRect(cf), Color.Lerp(color, Color.Transparent, curTween), angle, Origin, SpriteEffects.None, 0);
            spriteBatch.Draw(Texture, bounds, GetFrameRect(nf), Color.Lerp(color, Color.Transparent, nextTween), angle, Origin, SpriteEffects.None, 0);
        }

        public static (float, float) GetTween(float frameDelta, TweenStyle tween)
        {
            switch (tween)
            {
                case TweenStyle.Overlap:
                    return (MathHelper.Max((frameDelta * 2) - 1, 0), 
                            MathHelper.Max(1 - (frameDelta * 2), 0));
                case TweenStyle.Sequential:
                    return (frameDelta, 1 - frameDelta);
                default:
                    return (1, 0);
            }
        }

        /// <summary>
        /// Calculate the dest rectangle to fit an image into a rectangle:
        /// Shrink if too large and center if too small to fit into the <see cref="Region"/>
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="region">The region to fit to</param>
        /// <returns>The fit rectangle</returns>
        public static Rectangle GetFitRect(int width, int height, Rectangle region)
        {
            width = MathHelper.Min(width, region.Width);
            height = MathHelper.Min(height, region.Height);
            return new Rectangle(region.X + MathHelper.Max(0, (region.Width - width) / 2), region.Y + MathHelper.Max(0, (region.Height - height) / 2), width, height);
        }
        /// <summary>
        /// Calculate the dest rectangle to contain an image into a rectangle:
        /// Shrink if too large and center if too small to fit into the <see cref="Region"/>
        /// while maintaining the same aspect ratio
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="region">The region to contain to</param>
        /// <returns>The contain rectangle</returns>
        public static Rectangle GetContainRect(int width, int height, Rectangle region)
        {
            //todo
            width = MathHelper.Min(width, region.Width);
            height = MathHelper.Min(height, region.Height);
            return new Rectangle(region.X + MathHelper.Max(0, (region.Width - width) / 2), region.Y + MathHelper.Max(0, (region.Height - height) / 2), width, height);
        }

        protected void DerivedDeserialize(System.Collections.Generic.Dictionary<string, object> props)
        {
            bool hasSize = props.ContainsKey("Size");

            if (!hasSize && !props.ContainsKey("Width"))
                Width = Texture?.Width ?? 1;
            if (!hasSize && !props.ContainsKey("Height"))
                Height = Texture?.Height ?? 1;

            if (props.TryGetValue("Center", out var center) &&
                center is bool doCenter && doCenter)
                CenterOrigin(); //maybe default this to true
        }
    }
}
