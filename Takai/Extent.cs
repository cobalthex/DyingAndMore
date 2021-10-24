using System;
using Microsoft.Xna.Framework;

namespace Takai
{
    public struct Extent
    {
        public Vector2 min, max;

        public Vector2 Size => max - min;

        public Extent(float left, float top, float right, float bottom)
        {
            min = new Vector2(left, top);
            max = new Vector2(right, bottom);
        }

        public Extent(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public Extent(Rectangle rect)
        {
            min = new Vector2(rect.Left, rect.Top);
            max = new Vector2(rect.Right, rect.Bottom);
        }

        public static Extent FromRect(Vector2 position, Vector2 size)
        {
            return new Extent(position, position + size);
        }

        public static implicit operator Extent((Vector2, Vector2) extent)
        {
            return new Extent(extent.Item1, extent.Item2);
        }

        public override string ToString()
        {
            return $"{{min:{min.X},{min.Y} max:{max.X},{max.Y}}}";
        }

        public Rectangle AsRectangle()
        {
            return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        }

        public bool Contains(Vector2 point)
        {
            return (point.X >= min.X && point.X <= max.X &&
                    point.Y >= min.Y && point.Y <= max.Y);
        }

        public static Extent Intersect(Extent a, Extent b)
        {
            var left   = Math.Max(a.min.X, b.min.X);
            var right  = Math.Min(a.max.X, b.max.X);
            var top    = Math.Max(a.min.Y, b.min.Y);
            var bottom = Math.Min(a.max.Y, b.max.Y);

            if (right >= left && bottom >= top)
                return new Extent(new Vector2(left, top), new Vector2(right, bottom));
            else
                return new Extent();
        }
    }
}
