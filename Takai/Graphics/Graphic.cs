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
        Simultaneous,
        /// <summary>
        /// The next frame is faded in before the previous frame is faded out
        /// </summary>
        Consecutive,
    }

    /// <summary>
    /// A graphic animation (A sprite)
    /// </summary>
    public class Graphic : Animation
    {
        #region Data

        /// <summary>
        /// The image to use
        /// </summary>
        public Texture2D image;

        /// <summary>
        /// The size of each frame
        /// </summary>
        public Point size;

        /// <summary>
        /// The bounds of the graphic (used in conjunction with placement)
        /// </summary>
        public Rectangle bounds;
        /// <summary>
        /// The width of the graphic
        /// </summary>
        public int width { get { return size.X; } }
        /// <summary>
        /// The height of the graphic
        /// </summary>
        public int height { get { return size.Y; } }

        /// <summary>
        /// The region to take the animation from in the image
        /// frameCount are taken by moving to the right row by row
        /// </summary>
        public Rectangle clipRect
        {
            get { return _clip; }
            set
            {
                if (value == Rectangle.Empty)
                    _clip = image.Bounds;
                else
                    _clip = value;
            }
        }
        Rectangle _clip;

        /// <summary>
        /// The specific placement of the graphic inside the bounds
        /// </summary>
        public GraphicPlacement placement;

        /// <summary>
        /// An optional hue to apply to the image
        /// </summary>
        public Color hue;

        /// <summary>
        /// Tween the animation (smooth between frameCount)
        /// </summary>
        public TweenStyle tween;

        /// <summary>
        /// The angle of the graphic
        /// </summary>
        public float angle;
        /// <summary>
        /// The origin for rotation
        /// </summary>
        public Vector2 origin;

        #endregion

        /// <summary>
        /// Create a default graphic
        /// </summary>
        public Graphic()
        {
            placement = GraphicPlacement.Center;
            size = Point.Zero;
            bounds = Rectangle.Empty;
            _clip = Rectangle.Empty;
            image = null;
            hue = Color.White;
            tween = TweenStyle.None;
            angle = 0;
            origin = Vector2.Zero;
        }

        public Graphic(Texture2D Image, Point FrameSize, Rectangle? Bounds, Rectangle? ClipRect, uint NumFrames, System.TimeSpan FrameLength,
            AnimationOptions AnimationOptions, TweenStyle TweenStyle = TweenStyle.Consecutive)
            : base(NumFrames, FrameLength, AnimationOptions)
        {
            image = Image;
            size = FrameSize;
            bounds = Bounds == null ? new Rectangle(0, 0, FrameSize.X, FrameSize.Y) : Bounds.Value;
            hue = Color.White;
            placement = GraphicPlacement.Center;
            angle = 0;
            origin = Vector2.Zero;
            tween = TweenStyle;

            if (ClipRect != null)
                _clip = ClipRect.Value;
            else if (image == null)
                _clip = Rectangle.Empty;
            else
                _clip = image.Bounds;
        }

        /// <summary>
        /// Create a static graphic
        /// </summary>
        /// <param name="Image">The image to use</param>
        /// <param name="Bounds">The position and size of the image</param>
        /// <param name="ClipRect">The portion of the image to use</param>
        public Graphic(Texture2D Image, Rectangle? Bounds = null, Rectangle? ClipRect = null)
            : base(0, System.TimeSpan.Zero, AnimationOptions.None)
        {
            image = Image;
            size = new Point(Image.Bounds.Width, Image.Bounds.Height);
            bounds = Bounds == null ? new Rectangle(0, 0, Image.Bounds.Width, Image.Bounds.Height) : Bounds.Value;
            hue = Color.White;
            placement = GraphicPlacement.Center;
            tween = TweenStyle.None;
            angle = 0;
            origin = Vector2.Zero;

            if (ClipRect != null)
                _clip = ClipRect.Value;
            else if (image == null)
                _clip = Rectangle.Empty;
            else
                _clip = image.Bounds;
        }

        /// <summary>
        /// Center the origin of the sprite
        /// </summary>
        public void CenterOrigin()
        {
            origin = size.ToVector2() / 2;
        }

        /// <summary>
        /// Export the graphic's information (not texture) as a single packed string
        /// </summary>
        /// <returns>The packed string</returns>
        public string Export()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Import a graphic from a string and a texture
        /// </summary>
        /// <param name="Texture"></param>
        /// <param name="Metadata"></param>
        /// <returns>The created graphic</returns>
        public static Graphic Import(Texture2D Texture, string Metadata)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Duplicate the graphic
        /// </summary>
        /// <returns>A duplicated version of the graphic</returns>
        public Graphic Clone()
        {
            Graphic g = (Graphic)MemberwiseClone();

            if (IsRunning)
                g.Restart();
            else
                g.Reset();

            return g;
        }

        /// <summary>
        /// Draw the sprite at its current frame
        /// </summary>
        /// <param name="SpriteBatch">Spritebatch to use</param>
        public void Draw(SpriteBatch SpriteBatch)
        {
            Draw(SpriteBatch, bounds, angle);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position)
        {
            Draw(SpriteBatch, Position, angle);
        }

        public void Draw(SpriteBatch SpriteBatch, Vector2 Position, float Angle)
        {
            var bnd = new Rectangle((int)Position.X, (int)Position.Y, bounds.Width, bounds.Height);
            Draw(SpriteBatch, bnd, Angle);
        }

        /// <summary>
        /// Draw the sprite at a specific location at its current frame
        /// </summary>
        /// <param name="SpriteBatch">Spritebatch to use</param>
        /// <param name="Bounds">Specific location to draw at</param>
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds)
        {
            Draw(SpriteBatch, Bounds, angle);
        }

        /// <summary>
        /// Draw the sprite at a specific location at its current frame
        /// </summary>
        /// <param name="SpriteBatch">Spritebatch to use</param>
        /// <param name="Bounds">Specific location to draw at</param>
        /// <param name="Angle">The angle to draw the image at</param>
        public void Draw(SpriteBatch SpriteBatch, Rectangle Bounds, float Angle)
        {
            if (image == null)
                throw new System.ArgumentNullException("Texture cannot be null");
            if (frameCount < 2) //no animation
            {
                DrawImage(image, SpriteBatch, placement, Bounds, Angle, origin, _clip, hue);
                return;
            }

            int row = _clip.Width / size.X;

            if (tween != TweenStyle.None && (isLooping || currentFrame < frameCount))
            {
                //current frame and next frame
                float cf = MathHelper.Clamp(((float)(Elapsed - offset).Ticks / frameLength.Ticks) % frameCount, 0, frameCount + 1);
                int nf = ((int)cf + 1) % (int)frameCount;

                //regions of current and next frame
                Rectangle rgn = new Rectangle(_clip.X + ((size.X * (int)cf) % _clip.Width), _clip.Y + ((int)cf / row) * size.Y, size.X, size.Y);
                Rectangle rgn2 = new Rectangle(_clip.X + ((size.X * nf) % _clip.Width), _clip.Y + (nf / row) * size.Y, size.X, size.Y);

                if (tween == TweenStyle.Simultaneous)
                {
                    Color c = hue;
                    float dif = cf - (int)cf; //0-1 percentage between frameCount
                    c.A = (byte)MathHelper.Lerp(0, hue.A, dif);
                    if (isLooping || currentFrame < frameCount - 1)
                        DrawImage(image, SpriteBatch, placement, Bounds, Angle, origin, rgn2, c); //draw next frame

                    c.A = (byte)(MathHelper.Lerp(0, hue.A, 1 - (dif)));
                    DrawImage(image, SpriteBatch, placement, Bounds, Angle, origin, rgn, c); //draw current frame
                }
                else if (tween == TweenStyle.Consecutive)
                {
                    Color c = hue;
                    float dif = cf - (int)cf; //0-1 percentage between frameCount
                    if (dif <= 0.5f) //next frame
                        c.A = (byte)MathHelper.Lerp(0, hue.A, dif * 2);
                    if (isLooping || currentFrame < frameCount - 1)
                        DrawImage(image, SpriteBatch, placement, Bounds, Angle, origin, rgn2, c); //draw next frame

                    if (dif >= 0.5f) //current frame
                        c.A = (byte)(MathHelper.Lerp(0, hue.A, 1 - (dif * 2)));
                    else
                        c.A = (byte)hue.A;
                    DrawImage(image, SpriteBatch, placement, Bounds, Angle, origin, rgn, c); //draw current frame
                }
            }
            else
            {
                int cf = (int)currentFrame;
                Rectangle rgn = new Rectangle(_clip.X + ((size.X * cf) % _clip.Width), _clip.Y + (cf / row) * size.Y, size.X, size.Y);

                DrawImage(image, SpriteBatch, placement, Bounds, Angle, origin, rgn, hue);
            }
        }

        /// <summary>
        /// Draw an image with its placement settings
        /// </summary>
        /// <param name="Image">The image to draw</param>
        /// <param name="SpriteBatch">The spritebatch to draw with</param>
        /// <param name="Placement">The placement of the image</param>
        /// <param name="Region">The region of the image to pick from</param>
        /// <param name="Bounds">An optional boundary to draw to</param>
        /// <param name="Hue">An optional hue to apply to the image; white for no hue</param>
        public static void DrawImage(Texture2D Image, SpriteBatch SpriteBatch, GraphicPlacement Placement, Rectangle Bounds, Rectangle? Region, Color? Hue)
        {
            DrawImage(Image, SpriteBatch, Placement, Bounds, 0, Vector2.Zero, Region, Hue);
        }

        /// <summary>
        /// Draw an image with its placement settings
        /// </summary>
        /// <param name="Image">The image to draw</param>
        /// <param name="SpriteBatch">The spritebatch to draw with</param>
        /// <param name="Placement">The placement of the image</param>
        /// <param name="Region">The region of the image to pick from</param>
        /// <param name="Origin">The origin of rotation</param>
        /// <param name="Rotation">Rotational amount in radians</param>
        /// <param name="Bounds">An optional boundary to draw to</param>
        /// <param name="Hue">An optional hue to apply to the image; white for no hue</param>
        public static void DrawImage(Texture2D Image, SpriteBatch SpriteBatch, GraphicPlacement Placement, Rectangle Bounds, float Rotation, Vector2 Origin, Rectangle? Region, Color? Hue)
        {
            if (Image == null)
                return;

            Rectangle b = Bounds;

            Color hu = Hue == null ? Color.White : (Color)Hue;
            Rectangle rgn = (Region == null || Region.Value == Rectangle.Empty ? Image.Bounds : (Rectangle)Region);

            //draw based on placement
            if (Placement == GraphicPlacement.None) //None
            {
                int lesx = (rgn.Width < b.Width ? rgn.Width : b.Width);
                int lesy = (rgn.Height < b.Height ? rgn.Height : b.Height);
                SpriteBatch.Draw(Image, new Rectangle(b.X, b.Y, lesx, lesy), new Rectangle(rgn.X, rgn.Y, lesx, lesy), hu, Rotation, Origin, SpriteEffects.None, 0);
            }

            else if (Placement == GraphicPlacement.Center) //Centered
            {
                //calc center and don't let dimensions bigger than bounds
                Rectangle r = rgn;
                int wd = rgn.Width;
                int hd = rgn.Height;
                if (rgn.Width > b.Width)
                {
                    wd = b.Width - r.Width;
                    r.X = rgn.X - (wd >> 1);
                    r.Width = b.Width;
                }
                if (rgn.Height > b.Height)
                {
                    hd = b.Height - r.Height;
                    r.Y = rgn.Y - (hd >> 1);
                    r.Height = b.Height;
                }

                SpriteBatch.Draw(Image, new Rectangle(b.X + (b.Width - (wd < b.Width ? r.Width : b.Width) >> 1), b.Y + (b.Height - (r.Height < b.Height ? r.Height : b.Height) >> 1), r.Width, r.Height),
                    r, hu, Rotation, Origin, SpriteEffects.None, 0);
            }

            else if (Placement == GraphicPlacement.CenterStretch) //Stretched if larger, centered if smaller
            {
                Rectangle loc = new Rectangle(b.X, b.Y, b.Width, b.Height);
                //b.X + (b.Width - (wd < b.Width ? r.Width : b.Width) >> 1), b.Y + (b.Height - (r.Height < b.Height ? r.Height : b.Height) >> 1)

                if (rgn.Width < b.Width)
                {
                    int n = b.Width - rgn.Width;
                    loc.X += n >> 1;
                    loc.Width -= n;
                }
                if (rgn.Height < b.Height)
                {
                    int n = b.Height - rgn.Height;
                    loc.Y += n >> 1;
                    loc.Height -= n;
                }

                SpriteBatch.Draw(Image, loc, rgn, hu, Rotation, Origin, SpriteEffects.None, 0);
            }

            else if (Placement == GraphicPlacement.Tile) //Tile
            {
                //Calc max number of visible items in region
                int xr = (int)System.Math.Ceiling((float)b.Width / rgn.Width);
                int yr = (int)System.Math.Ceiling((float)b.Height / rgn.Height);

                //calculate the size when clipped
                int clipx = rgn.Width - ((xr * rgn.Width) - b.Width);
                int clipy = rgn.Height - ((yr * rgn.Height) - b.Height);

                for (int x = 0; x < xr; x++)
                    for (int y = 0; y < yr; y++)
                    {
                        //calculate boundaries, but clip if image dimensions exceed bounds
                        Rectangle bd = new Rectangle(b.X + x * rgn.Width, b.Y + y * rgn.Height, (x + 1) * rgn.Width > b.Width ? clipx : rgn.Width,
                            (y + 1) * rgn.Height > b.Height ? clipy : rgn.Height);

                        SpriteBatch.Draw(Image, bd, new Rectangle(rgn.X, rgn.Y, bd.Width, bd.Height), hu, Rotation, Origin, SpriteEffects.None, 0);
                    }
            }

            else if (Placement == GraphicPlacement.Stretch) //Stretch
                SpriteBatch.Draw(Image, b, rgn, hu, Rotation, Origin, SpriteEffects.None, 0);

            else if (Placement == GraphicPlacement.Fit) //Fit
            { throw new System.NotImplementedException(); }
        }
    }


    /// <summary>
    /// Specify how an graphic is placed in a specific region
    /// </summary>
    public enum GraphicPlacement : byte
    {
        /// <summary>
        /// Default placement
        /// </summary>
        None = 0,
        /// <summary>
        /// Center the image (does not fit to dimensions)
        /// </summary>
        Center,
        /// <summary>
        /// Center the image if the region is larger than the image or stretch the image if the region is smaller
        /// </summary>
        CenterStretch,
        /// <summary>
        /// Tile the image, if larger than dimensions, draws with top left orientation
        /// </summary>
        Tile,
        /// <summary>
        /// Stretch the image to fit the dimensions
        /// </summary>
        Stretch,
        /// <summary>
        /// Fit the image to the dimensions without frameLength/width size ratio
        /// 
        /// NOT IMPLEMENTED
        /// </summary>
        Fit
    }
}
