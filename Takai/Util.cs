using System;
using Microsoft.Xna.Framework;

namespace Takai
{
    public static class Util
    {
        public static readonly Random RandomGenerator = new Random();

        public static T Random<T>(this System.Collections.Generic.IList<T> list) //distribution?
        {
            if (list == null || list.Count < 1)
                return default(T);

            return list[Util.RandomGenerator.Next(list.Count)];
        }

        public static Vector2 RandomCircle(Vector2 position, float radius)
        {
            //todo: replace all other locations with this (particles?)
            return position + new Vector2(
                (float)Math.Cos(RandomGenerator.NextDouble() * MathHelper.TwoPi),
                (float)Math.Sin(RandomGenerator.NextDouble() * MathHelper.TwoPi)
            ) * radius;
        }

        /// <summary>
        /// Return a gaussian distribution of a random double
        /// </summary>
        /// <param name="random">The random generator</param>
        /// <param name="mean">The mean value of the curve</param>
        /// <param name="standardDeviation">The standard deviation of the curve</param>
        /// <returns>The gaussian distributed random</returns>
        public static double NextGaussian(this Random random, double mean = 0, double standardDeviation = 1)
        {
            //todo: use carry to avoid trig
            //todo: zigurat algorithm

            //box muller formula
            var u1 = 1.0 - random.NextDouble(); //uniform(0,1] random doubles
            var u2 = 1.0 - random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return mean + standardDeviation * randStdNormal; //random normal(mean,stdDev^2)
        }

        /// <summary>
        /// Resize an array
        /// </summary>
        /// <typeparam name="T">The array type</typeparam>
        /// <param name="original">The original array to copy data from</param>
        /// <param name="rows">the new number of rows</param>
        /// <param name="columns">the new number of columns</param>
        /// <returns>The resized array, or the original array if the size is unchanged</returns>
        public static T[,] Resize<T>(this T[,] original, int rows, int columns)
        {
            if (original.GetLength(0) == rows &&
                original.GetLength(1) == columns)
                return original;

            T[,] newArray = new T[rows, columns];
            int minRows = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minCols = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minCols; ++i)
                Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minRows);

            return newArray;
        }

        /// <summary>
        /// Resize a list
        /// </summary>
        /// <typeparam name="T">The type of values in the list</typeparam>
        /// <param name="list">The list to resize</param>
        /// <param name="newSize">The new size of the list</param>
        /// <param name="defaultValue">A default value for any new items in the list. Existing items will be unchanged</param>
        public static void Resize<T>(this System.Collections.Generic.List<T> list, int newSize, T defaultValue = default(T))
        {
            var oldSize = list.Count;
            if (newSize < oldSize)
                list.RemoveRange(newSize, oldSize - newSize);
            for (int i = oldSize; i < newSize; ++i)
                list.Add(defaultValue);
        }

        public static Vector2 Reject(this Vector2 a, Vector2 b)
        {
            var ab = Vector2.Dot(a, b);
            var bb = Vector2.Dot(b, b);
            return a - (b * (ab / bb));
        }

        public static float Angle(this Vector2 v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }

        public static Vector2 Direction(float angle)
        {
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
        
        public static int CeilDiv(int n, int m)
        {
            return (n / m) + ((n % m) > 0 ? 1 : 0);
        }

        public static float Determinant(Vector2 a, Vector2 b)
        {
            return (a.X * b.Y) - (a.Y * b.X);
        }

        public static Vector2 Round(this Vector2 v)
        {
            return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        }

        public static Vector2 Sign(this Vector2 v)
        {
            return new Vector2(Math.Sign(v.X), Math.Sign(v.Y));
        }

        public static Vector2 Ceiling(this Vector2 v)
        {
            return new Vector2((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y));
        }

        /// <summary>
        /// Calculate a rectangle that is always of positive size
        /// </summary>
        /// <param name="a">One corner of the rectangle</param>
        /// <param name="b">The other corner of the rectangle</param>
        /// <returns></returns>
        public static Rectangle AbsRectangle(Point a, Point b)
        {
            Point c = b, d = a;
            if (a.X < b.X)
            {
                c.X = a.X;
                d.X = b.X;
            }
            if (a.Y < b.Y)
            {
                c.Y = a.Y;
                d.Y = b.Y;
            }

            return new Rectangle(c.X, c.Y, d.X - c.X, d.Y - c.Y);
        }

        public static Rectangle AbsRectangle(Vector2 a, Vector2 b)
        {
            return AbsRectangle(a.ToPoint(), b.ToPoint());
        }

        /// <summary>
        /// Clamp <see cref="rect"/> to <see cref="container"/> without resizing (overflow will happen if rect is larger than container)
        /// </summary>
        /// <param name="rect">The rect to contain</param>
        /// <param name="container">The container to contain the <see cref="rect"/></param>
        /// <returns>The adjusted position of <see cref="rect"/></returns>
        public static Point Clamp(Rectangle rect, Rectangle container)
        {
            if (rect.X < container.X)
                rect.X = container.X;
            else if (rect.Right > container.Right)
                rect.X = container.Width - rect.Width;

            if (rect.Y < container.Y)
                rect.Y = container.Y;
            else if (rect.Bottom > container.Bottom)
                rect.Y = container.Height - rect.Height;

            return rect.Location;
        }

        /// <summary>
        /// Return a vector orthagonal to this one
        /// </summary>
        /// <param name="v">The vector to use</param>
        /// <returns>An orthagonal vector</returns>
        public static Vector2 Ortho(this Vector2 v)
        {
            return new Vector2(v.Y, -v.X);
        }

        public static Point Max(Point a, Point b)
        {
            return new Point(
                Math.Max(a.X, b.X),
                Math.Max(a.Y, b.Y)
            );
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min ? min : (value > max ? max : value));
        }
        public static float Clamp(float value, float min, float max)
        {
            return (value < min ? min : (value > max ? max : value));
        }

        public static TimeSpan Max(TimeSpan a, TimeSpan b)
        {
            return TimeSpan.FromTicks(Math.Max(a.Ticks, b.Ticks));
        }

        public static TimeSpan Min(TimeSpan a, TimeSpan b)
        {
            return TimeSpan.FromTicks(Math.Min(a.Ticks, b.Ticks));
        }

        public static string PrettyPrintMatrix(Matrix m)
        {
            return $"┌{m.M11,5} {m.M12,5} {m.M13,5} {m.M14,5}┐\n" +
                   $"│{m.M21,5} {m.M22,5} {m.M23,5} {m.M24,5}│\n" +
                   $"│{m.M31,5} {m.M32,5} {m.M33,5} {m.M34,5}│\n" +
                   $"└{m.M41,5} {m.M42,5} {m.M43,5} {m.M44,5}┘\n";
        }

        public static bool PassChance(float passPercent)
        {
            return passPercent > RandomGenerator.NextDouble();
        }

        public static Point ToPoint(this Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }
        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }

        public static bool Contains(this Rectangle r, Vector2 v)
        {
            return r.Contains((int)v.X, (int)v.Y);
        }

        public static Point Relative(this Rectangle r, Point p)
        {
            return new Point(p.X - r.X, p.Y - r.Y);
        }

        public static Vector2 Relative(this Rectangle r, Vector2 v)
        {
            return new Vector2(v.X - r.X, v.Y - r.Y);
        }

        /// <summary>
        /// Convert an object name to a more locale-friendly name
        /// This includes adding spaces and correct capitalization
        /// </summary>
        /// <param name="name">the name to convert</param>
        /// <returns>The formatted name</returns>
        public static string ToSentenceCase(this string name)
        {
            //todo: internationalize

            if (string.IsNullOrWhiteSpace(name))
                return "";

            var builder = new System.Text.StringBuilder(name.Length + 4);
            builder.Append(char.ToUpper(name[0]));
            for (int i = 1; i < name.Length; ++i)
            {
                if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    builder.Append(' ');
                    builder.Append(char.ToLower(name[i]));
                }
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
        }

    }

    public struct Extent
    {
        public Vector2 min, max;

        public Extent(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(Vector2 point)
        {
            return (point.X >= min.X && point.X <= max.X &&
                    point.Y >= min.Y && point.Y <= max.Y);
        }
    }

    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}
