using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

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
        Overlapping,
        /// <summary>
        /// The next frame is faded in before the previous frame is faded out
        /// </summary>
        Sequentially,
    }

    /// <summary>
    /// A graphic animation (A sprite)
    /// </summary>
    public class Graphic : Animation
    {
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
            bool ShouldLoop,
            bool StartImmediately = true
        ) : base(FrameCount, FrameTime, ShouldLoop, StartImmediately)
        {
            this.Texture = Texture;
            this.Width = Width;
            this.Height = Height;
            this.ClipRect = Texture.Bounds;
            this.Origin = Vector2.Zero;
            this.Tween = TweenStyle.None;
        }

        public Graphic
        (
            Texture2D Texture,
            int Width,
            int Height,
            Rectangle ClipRect,
            int FrameCount,
            System.TimeSpan FrameTime,
            bool ShouldLoop,
            bool StartImmediately = true
        ) : base(FrameCount, FrameTime, ShouldLoop, StartImmediately)
        {
            this.Texture = Texture;
            this.Width = Width;
            this.Height = Height;
            this.ClipRect = ClipRect;
            this.Origin = Vector2.Zero;
            this.Tween = TweenStyle.None;
        }
        
        public new Graphic Clone()
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
            return new Rectangle((Frame % framesPerRow) * Width, (Frame / framesPerRow) * Height, Width, Height);
        }

        //todo: tweening

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position)
        {
            SpriteBatch.Draw(Texture, Position, GetFrameRect(CurrentFrame), Color.White);
        }
        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, Color Color)
        {
            SpriteBatch.Draw(Texture, Position, GetFrameRect(CurrentFrame), Color);
        }
        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle)
        {
            SpriteBatch.Draw(Texture, Position, GetFrameRect(CurrentFrame), Color.White, Angle, Origin, 1, SpriteEffects.None, 0);
        }
        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle, Color Color)
        {
            SpriteBatch.Draw(Texture, Position, GetFrameRect(CurrentFrame), Color, Angle, Origin, 1, SpriteEffects.None, 0);
        }

        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds)
        {
            SpriteBatch.Draw(Texture, Bounds, GetFrameRect(CurrentFrame), Color.White);
        }
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, Color Color)
        {
            SpriteBatch.Draw(Texture, Bounds, GetFrameRect(CurrentFrame), Color);
        }
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle)
        {
            SpriteBatch.Draw(Texture, Bounds, GetFrameRect(CurrentFrame), Color.White, Angle, Origin, SpriteEffects.None, 0);
        }
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle, Color Color)
        {
            SpriteBatch.Draw(Texture, Bounds, GetFrameRect(CurrentFrame), Color, Angle, Origin, SpriteEffects.None, 0);
        }
    }
}
