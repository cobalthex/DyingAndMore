namespace Takai
{
    /// <summary>
    /// A simple floating point rectangle
    /// (used mainly for UV coordinates)
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct FRectangle
    {
        /// <summary>
        /// X coordinate of this rectangle
        /// </summary>
        public float x;
        /// <summary>
        /// Y coordinate of this rectangle
        /// </summary>
        public float y;
        /// <summary>
        /// Width of this rectangle
        /// </summary>
        public float width;
        /// <summary>
        /// Height of this rectangle
        /// </summary>
        public float height;

        /// <summary>
        /// Gets or sets the top value of this rectangle
        /// </summary>
        public float top
        {
            get { return y; }
            set { var oldY = y; y = value; height += oldY - y; }
        }
        /// <summary>
        /// Gets or sets the bottom value of this rectangle
        /// </summary>
        public float bottom
        {
            get { return y + height; }
            set { height = value - y; }
        }
        /// <summary>
        /// Gets or sets the left value of this rectangle
        /// </summary>
        public float left
        {
            get { return x; }
            set { var oldX = x; x = value; width += oldX - x; }
        }
        /// <summary>
        /// Gets or sets the right value of this rectangle
        /// </summary>
        public float right
        {
            get { return x + width; }
            set { width = value - x; }
        }

        public Microsoft.Xna.Framework.Vector2 topLeft
        {
            get { return new Microsoft.Xna.Framework.Vector2(x, y); }
            set
            {
                var oldX = x; x = value.X; width += oldX - x;
                var oldY = y; y = value.Y; height += oldY - y;
            }
        }

        public Microsoft.Xna.Framework.Vector2 topRight
        {
            get { return new Microsoft.Xna.Framework.Vector2(x + width, y); }
            set
            {
                width = value.X - x;
                var oldY = y; y = value.Y; height += oldY - y;
            }
        }

        public Microsoft.Xna.Framework.Vector2 bottomRight
        {
            get { return new Microsoft.Xna.Framework.Vector2(x + width, y + height); }
            set { width = value.X - x; height = value.Y - y; }
        }

        public Microsoft.Xna.Framework.Vector2 bottomLeft
        {
            get { return new Microsoft.Xna.Framework.Vector2(x, y + height); }
            set
            {
                var oldX = x; x = value.X; width += oldX - x;
                height = value.Y - y;
            }
        }

        /// <summary>
        /// An empty rectangle
        /// </summary>
        public static FRectangle Empty { get { return new FRectangle(0, 0, 0, 0); } }

        /// <summary>
        /// Create a new floating point rectangle
        /// </summary>
        /// <param name="X">X component of the rectangle</param>
        /// <param name="Y">Y component of the rectangle</param>
        /// <param name="Width">Width of the rectangle</param>
        /// <param name="Height">Height of the rectangle</param>
        public FRectangle(float X, float Y, float Width, float Height)
        {
            this.x = X;
            this.y = Y;
            this.width = Width;
            this.height = Height;
        }

        /// <summary>
        /// Create a floating point rectangle from an int based rectangle and a size component
        /// Typically used to create UV rectangles (texcoords) where the rectangle is a portion of the image and MaxWidth and MaxHeight are the size of the image
        /// </summary>
        /// <param name="Rectangle">The rectangle to scale</param>
        /// <param name="MaxWidth">The maximum width</param>
        /// <param name="MaxHeight">The maximum height</param>
        /// <returns>A floating point rectangle from the specified rectangle</returns>
        public static FRectangle FromRectangle(Microsoft.Xna.Framework.Rectangle Rectangle, int MaxWidth, int MaxHeight)
        {
            FRectangle f = new FRectangle();

            f.x = (float)Rectangle.X / MaxWidth;
            f.y = (float)Rectangle.Y / MaxHeight;
            f.width = (float)Rectangle.Width / MaxWidth;
            f.height = (float)Rectangle.Height / MaxHeight;
            
            return f;
        }

        public Microsoft.Xna.Framework.Rectangle ToRectangle(Microsoft.Xna.Framework.Rectangle Scalar)
        {
            Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle();

            r.X = (int)(x * Scalar.Width + Scalar.X);
            r.Y = (int)(y * Scalar.Height + Scalar.Y);
            r.Width = (int)(Scalar.Width * width);
            r.Height = (int)(Scalar.Height * height);

            return r;
        }

        /// <summary>
        /// Inflate a rectangle
        /// </summary>
        /// <param name="X">How far to push in/out the horizontal sides</param>
        /// <param name="Y">How far to push in/out the vertical sides</param>
        public void Inflate(float X, float Y)
        {
            x -= X;
            y -= Y;
            width += X * 2;
            height += Y * 2;
        }

        /// <summary>
        /// Offset the rectangle by a specified amount
        /// </summary>
        /// <param name="X">How far to move the X component</param>
        /// <param name="Y">How far to move the Y component</param>
        public void Offset(float X, float Y)
        {
            x += X;
            y += Y;
        }

        /// <summary>
        /// Are two floating rectangles the same?
        /// </summary>
        /// <param name="a">Rectangle A</param>
        /// <param name="b">Rectangle B</param>
        /// <returns>True if all values are identical</returns>
        public static bool operator==(FRectangle a, FRectangle b)
        {
            if (a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height)
                return true;
            return false;
        }

        /// <summary>
        /// Are two floating rectangles the same?
        /// </summary>
        /// <param name="obj">The rectangle to compare</param>
        /// <returns>True if all values are identical</returns>
        public override bool Equals(object obj)
        {
            var b = (FRectangle)obj;
            if (x == b.x && y == b.y && width == b.width && height == b.height)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{X:{0} Y:{1} Width:{2} Height:{3}}}", x, y, width, height);
        }

        /// <summary>
        /// Are two floating rectangles different
        /// </summary>
        /// <param name="a">Rectangle A</param>
        /// <param name="b">Rectangle B</param>
        /// <returns>True if any values are not identical</returns>
        public static bool operator !=(FRectangle a, FRectangle b)
        {
            if (a.x == b.x || a.y == b.y || a.width == b.width || a.height == b.height)
                return false;
            return true;
        }
    }
}
