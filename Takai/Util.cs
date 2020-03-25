using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Takai
{
    public static class Util
    {
        static Util()
        {
            var clone = typeof(object).GetMethod("MemberwiseClone", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            _ShallowClone_Slow = (CloneFn)clone.CreateDelegate(typeof(CloneFn));
        }

        public static uint NextPowerOf2(uint n)
        {
            --n;
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            ++n;
            return n;
        }

        public static bool IsPowerOf2(int n)
        {
            return (n != 0 && 0 == (n & (n - 1)));
        }

        public static bool IsPowerOf2(long n)
        {
            return (n != 0 && 0 == (n & (n - 1)));
        }

        public static readonly Random RandomGenerator = new Random();

        public static T Random<T>(this System.Collections.Generic.IList<T> list) //distribution?
        {
            if (list == null || list.Count < 1)
                return default;

            return list[RandomGenerator.Next(list.Count)];
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

        public static T[,] Copy<T>(this T[,] original, Rectangle area)
        {
            var newArray = new T[
                Math.Min(area.Right, original.GetLength(1)) - area.X,
                Math.Min(area.Bottom, original.GetLength(0)) - area.Y
            ];

            for (int i = 0; i < newArray.GetLength(0); ++i)
            {
                Array.Copy(
                    original,
                    (i + area.X) * original.GetLength(1), 
                    newArray, 
                    i * newArray.GetLength(1), 
                    newArray.GetLength(1)
                );
            }

            return newArray;
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

        /// <summary>
        /// Resize a list
        /// </summary>
        /// <typeparam name="T">The type of values in the list</typeparam>
        /// <param name="array">The list to resize</param>
        /// <param name="newSize">The new size of the list</param>
        /// <param name="defaultValue">A default value for any new items in the list. Existing items will be unchanged</param>
        public static T[] EnsureSize<T>(this T[] array, int newSize, T defaultValue = default(T))
        {
            if (newSize > array.Length)
            {
                var newArray = new T[newSize];
                Array.Copy(array, newArray, newSize);
                for (int i = array.Length; i < newSize; ++i)
                    newArray[i] = defaultValue;
                return newArray;
            }
            return array;
        }

        public static T SwapAndDrop<T>(this System.Collections.Generic.IList<T> list, int index)
        {
            if (index < 0 || index >= list.Count)
                throw new IndexOutOfRangeException($"{nameof(index)} must be within the bounds of {nameof(list)}");

            var drop = list[index];
            list[index] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return drop;
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

        public static Point Abs(this Point p)
        {
            return new Point(Math.Abs(p.X), Math.Abs(p.Y));
        }
        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
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
            return $"┌{m.M11,8:N3} {m.M12,8:N3} {m.M13,8:N3} {m.M14,8:N3}┐\n" +
                   $"│{m.M21,8:N3} {m.M22,8:N3} {m.M23,8:N3} {m.M24,8:N3}│\n" +
                   $"│{m.M31,8:N3} {m.M32,8:N3} {m.M33,8:N3} {m.M34,8:N3}│\n" +
                   $"└{m.M41,8:N3} {m.M42,8:N3} {m.M43,8:N3} {m.M44,8:N3}┘\n";
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

        const string randChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; //first 62 chars of base64

        public static string RandomString(int minLength = 8, int maxLength = 8, string prefix = null)
        {
            var len = RandomGenerator.Next(minLength, maxLength + 1);
            var sb = new System.Text.StringBuilder(prefix, len);
            for (int i = 0; i < len; ++i)
                sb.Append(randChars[RandomGenerator.Next(0, randChars.Length)]);
            return sb.ToString();
        }

        public static string Ellipsis(this string str, int maxLength)
        {
            if (str.Length > maxLength)
                return str.Substring(0, maxLength - 1) + '…';
            return str;
        }

        /// <summary>
        /// Append _# to name (or increment #).
        /// e.g. foo -> foo_1 -> foo_2.
        /// Note: uses regex
        /// </summary>
        /// <param name="name">The name to increment</param>
        /// <param name="divider">The divider between the name and the number (default _)</param>
        /// <param name="initial">The initial number to append</param>
        /// <returns>The new name</returns>
        public static string IncrementName(string name, string divider = "_", int initial = 1)
        {
            var match = Regex.Match(name, divider + "(\\d+)$");
            if (match.Success)
                return name.Substring(0, match.Groups[1].Index) + (int.Parse(match.Groups[1].Value) + 1).ToString();
            else
                return name + divider + initial.ToString();
        }

        public static float LineIntersectAaBbX(Vector2 a, Vector2 b, float xLine)
        {
            return a.Y + (((b.Y - a.Y) / (b.X - a.X)) * (xLine - a.X));
        }
        public static float LineIntersectAaBbY(Vector2 a, Vector2 b, float yLine)
        {
            return a.X + ((yLine - a.Y) / ((b.Y - a.Y) / (b.X - a.X)));
        }

        public delegate object CloneFn(object source);
        public static CloneFn _ShallowClone_Slow { get; }
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
