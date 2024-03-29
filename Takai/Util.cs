﻿using System;
using System.Collections;
using System.Collections.Generic;
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
        public static int NextPowerOf2(int n)
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

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        public static readonly Random RandomGenerator = new Random();

        public static T Random<T>(this IList<T> list) //distribution?
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
        public static void Resize<T>(this List<T> list, int newSize, T defaultValue = default(T))
        {
            var oldSize = list.Count;
            if (newSize < oldSize)
                list.RemoveRange(newSize, oldSize - newSize);
            for (int i = oldSize; i < newSize; ++i)
                list.Add(defaultValue);
        }

        /// <summary>
        /// Ensure an array is of sufficient size
        /// </summary>
        /// <typeparam name="T">The type of values in the array</typeparam>
        /// <param name="array">The array to resize, if null, returns a new array of correct size</param>
        /// <param name="newSize">The new size of the array</param>
        /// <param name="defaultValue">A default value for any new items in the array. Existing items will be unchanged</param>
        public static T[] EnsureSize<T>(this T[] array, int newSize, T defaultValue = default(T))
        {
            if (array == null)
            {
                var newArray = new T[newSize];
                for (int i = 0; i < newSize; ++i)
                    newArray[i] = defaultValue;
                return newArray;
            }

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

        /// <summary>
        /// Ensure an list is of sufficient size
        /// </summary>
        /// <typeparam name="T">The type of values in the list</typeparam>
        /// <param name="list">The list to resize, if null, returns a new list of correct size</param>
        /// <param name="newSize">The new size of the list</param>
        /// <param name="defaultValue">A default value for any new items in the list. Existing items will be unchanged</param>
        public static List<T> EnsureSize<T>(this List<T> list, int newSize, T defaultValue = default(T))
        {
            if (list == null)
            {
                var newList = new List<T>();
                newList.Resize(newSize, defaultValue);
                return newList;
            }

            if (newSize > list.Count)
            {
                var newList = new List<T>(newSize);
                newList.AddRange(list);
                for (int i = list.Count; i < newSize; ++i)
                    newList[i] = defaultValue;
                return newList;
            }
            return list;
        }

        public static T SwapAndDrop<T>(this IList<T> list, int index)
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

        public static Vector2 Floor(this Vector2 v)
        {
            return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
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

        public static Rectangle Capture(Rectangle rectangle, Point p)
        {
            var left = rectangle.Left;
            var right = rectangle.Right;
            var top = rectangle.Top;
            var bottom = rectangle.Bottom;
            if (p.X < left)
                left = p.X;
            if (p.Y < top)
                top = p.Y;
            if (p.X > right)
                right = p.X;
            if (p.Y > bottom)
                bottom = p.Y;
            return new Rectangle(left, top, right - left, bottom - top);
        }

        public static float Diagonal(Rectangle rect)
        {
            return (float)Math.Sqrt(rect.Width * rect.Width + rect.Height * rect.Height);
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

        // todo: break this class up

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
                if ((char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) ||
                    (!char.IsDigit(name[i - 1]) && char.IsDigit(name[i])))
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

        [Flags]
        enum LineClipRegion
        {
            Inside = 0b0000,
            Left   = 0b0001,
            Right  = 0b0010,
            Bottom = 0b0100,
            Top    = 0b1000
        }
        static LineClipRegion GetLineClipRegion(Vector2 v, Rectangle r)
        {
            var region = LineClipRegion.Inside;

            if (v.X < r.Left)
                region |= LineClipRegion.Left;
            else if (v.X > r.Right)
                region |= LineClipRegion.Right;

            if (v.Y < r.Top)
                region |= LineClipRegion.Top;
            else if (v.Y > r.Bottom)
                region |= LineClipRegion.Bottom;

            return region;
        }

        public static bool ClipLine(ref Vector2 a, ref Vector2 b, Rectangle box) //floating point rect?
        {
            //cohen-sutherland

            var aRegion = GetLineClipRegion(a, box);
            var bRegion = GetLineClipRegion(b, box);
            var diff = b - a;
            
            while (true)
            {
                if ((aRegion | bRegion) == LineClipRegion.Inside)
                    return true;
                else if ((aRegion & bRegion) > LineClipRegion.Inside)
                    return false;
                else
                {
                    var regionOut = (aRegion != LineClipRegion.Inside ? aRegion : bRegion);
                    Vector2 p = Vector2.Zero;

                    if ((regionOut & LineClipRegion.Top) > 0)
                    {
                        // point is above the clip rectangle 
                        p.X = a.X + diff.X * (box.Top - a.Y) / diff.Y;
                        p.Y = box.Top;
                    }
                    else if ((regionOut & LineClipRegion.Bottom) > 0)
                    {
                        // point is below the rectangle 
                        p.X = a.X + diff.X * (box.Bottom - a.Y) / diff.Y;
                        p.Y = box.Bottom;
                    }
                    else if ((regionOut & LineClipRegion.Right) > 0)
                    {
                        // point is to the right of rectangle 
                        p.Y = a.Y + diff.Y * (box.Right - a.X) / diff.X;
                        p.X = box.Right;
                    }
                    else if ((regionOut & LineClipRegion.Left) > 0)
                    {
                        // point is to the left of rectangle 
                        p.Y = a.Y + diff.Y * (box.Left - a.X) / diff.X;
                        p.X = box.Left;
                    }

                    // intersection point found, replace repsective location with clipped
                    if (regionOut == aRegion)
                    {
                        a = p;
                        aRegion = GetLineClipRegion(a, box);
                    }
                    else
                    {
                        b = p;
                        bRegion = GetLineClipRegion(b, box);
                    }
                }
            }
        }

        public delegate object CloneFn(object source);
        public static CloneFn _ShallowClone_Slow { get; }

        public static string ToString(this BitArray bitArray)
        {
            var sb = new System.Text.StringBuilder(bitArray.Length);
            foreach (var bit in bitArray)
                sb.Append((bool)bit ? 1 : 0);
            return sb.ToString();
        }

        public static T? TryLerp<T>(T? a, T? b, Func<T, T, float, T> lerper, float t)
            where T : struct
        {
            if (!a.HasValue)
                return b;
            if (!b.HasValue)
                return a;

            return lerper(a.Value, b.Value, t);
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
