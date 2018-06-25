using System;
using Microsoft.Xna.Framework;

namespace Takai
{
    public static class Util
    {
        public static readonly Random RandomGenerator = new Random();

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

        /// <summary>
        /// Return a vector orthagonal to this one
        /// </summary>
        /// <param name="v">The vector to use</param>
        /// <returns>An orthagonal vector</returns>
        public static Vector2 Ortho(this Vector2 v)
        {
            return new Vector2(v.Y, -v.X);
        }

        /// <summary>
        /// Convert an RGB color to HSL
        /// </summary>
        /// <param name="color">the RGB color to convert</param>
        /// <returns>The HSLA representation of the color</returns>
        public static Vector4 ColorToHSL(Color color)
        {
            float r = (color.R / 255f);
            float g = (color.G / 255f);
            float b = (color.B / 255f);

            float min = Math.Min(Math.Min(r, g), b);
            float max = Math.Max(Math.Max(r, g), b);
            float delta = max - min;

            float h = 0;
            float s = 0;
            float L = (max + min) / 2f;

            if (delta != 0)
            {
                if (L < 0.5f)
                {
                    s = delta / (max + min);
                }
                else
                {
                    s = delta / (2.0f - max - min);
                }


                if (r == max)
                    h = (g - b) / delta;
                else if (g == max)
                    h = 2f + (b - r) / delta;
                else if (b == max)
                    h = 4f + (r - g) / delta;
            }

            h *= 60;
            if (h < 0)
                h += 360;

            return new Vector4(h, s, L, color.A);
        }

        public static Color ColorFromHSL(Vector4 hsla)
        {
            return ColorFromHSL(hsla.X, hsla.Y, hsla.Z, hsla.W);
        }

        public static Color ColorFromHSL(float hue, float saturation, float lightness, float alpha = 255f)
        {
            hue = MathHelper.Clamp(hue, 0, 360);
            saturation = MathHelper.Clamp(saturation, 0, 1);
            lightness = MathHelper.Clamp(lightness, 0, 1);
            alpha /= 255f;

            if (saturation == 0)
                return new Color(lightness, lightness, lightness, alpha);

            float min, mid, max;
            if (lightness >= 0.5f)
            {
                max = lightness - (lightness * saturation) + saturation;
                min = lightness + (lightness * saturation) - saturation;
            }
            else
            {
                max = lightness + (lightness * saturation);
                min = lightness - (lightness * saturation);
            }

            var iSextant = (int)Math.Floor(hue / 60f);
            if (hue > 300)
                hue -= 360f;

            hue /= 60f;
            hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);

            if (iSextant % 2 == 0)
                mid = hue * (max - min) + min;
            else
                mid = min - hue * (max - min);

            switch (iSextant)
            {
                case 1:
                    return new Color(mid, max, min, alpha);
                case 2:
                    return new Color(min, max, mid, alpha);
                case 3:
                    return new Color(min, mid, max, alpha);
                case 4:
                    return new Color(mid, min, max, alpha);
                case 5:
                    return new Color(max, min, mid, alpha);
                default:
                    return new Color(max, mid, min, alpha);
            }
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

        public static Vector4 HSLReverseLerp(Vector4 a, Vector4 b, float t)
        {
            return new Vector4(
                (a.X < b.X)
                    ? MathHelper.Lerp(359 - a.X, b.X, t)
                    : MathHelper.Lerp(a.X, 359 - b.X, t),
                MathHelper.Lerp(a.Y, b.Y, t),
                MathHelper.Lerp(a.Z, b.Z, t),
                MathHelper.Lerp(a.W, b.W, t)
            );
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
    }
}
