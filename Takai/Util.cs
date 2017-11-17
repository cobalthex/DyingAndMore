using System;
using Microsoft.Xna.Framework;

namespace Takai
{
    public static class Util
    {
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

        public static Color ColorFromHSL(float hue, float saturation, float lightness, float alpha = 1f)
        {
            hue = MathHelper.Clamp(hue, 0, 360);
            saturation = MathHelper.Clamp(saturation, 0, 1);
            lightness = MathHelper.Clamp(lightness, 0, 1);

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
                MathHelper.Max(a.X, b.X),
                MathHelper.Max(a.Y, b.Y)
            );
        }
    }
}
