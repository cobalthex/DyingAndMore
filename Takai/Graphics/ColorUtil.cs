using System;
using Microsoft.Xna.Framework;

namespace Takai.Graphics
{
    public static class ColorUtil
    {
        /// <summary>
        /// A random, opaque color
        /// </summary>
        public static Color RandomColor
        {
            get
            {
                byte[] randCol = new byte[3];
                Util.RandomGenerator.NextBytes(randCol);

                return new Color(
                    randCol[0],
                    randCol[1],
                    randCol[2],
                    (byte)255
                );
            }
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

        public static Vector4 ColorToHSV(Color color)
        {
            float delta, min;
            float h = 0, s, v;

            min = Math.Min(Math.Min(color.R, color.G), color.B);
            v = Math.Max(Math.Max(color.R, color.G), color.B);
            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0;

            else
            {
                if (color.R == v)
                    h = (color.G - color.B) / delta;
                else if (color.G == v)
                    h = 2 + (color.B - color.R) / delta;
                else if (color.B == v)
                    h = 4 + (color.R - color.G) / delta;

                h *= 60;

                if (h < 0.0)
                    h = h + 360;
            }

            return new Vector4(h, s, (v / 255), color.A / 255f);
        }

        public static Color ColorFromHSV(Vector4 hsva)
        {
            float r = ((MathHelper.Clamp(Math.Abs(hsva.X * 6 - 3) - 1, 0, 1) - 1) * hsva.Y + 1) * hsva.Z;
            float g = ((MathHelper.Clamp(2 - Math.Abs(hsva.X * 6 - 2), 0, 1) - 1) * hsva.Y + 1) * hsva.Z;
            float b = ((MathHelper.Clamp(2 - Math.Abs(hsva.X * 6 - 4), 0, 1) - 1) * hsva.Y + 1) * hsva.Z;

            return new Color(r, g, b, hsva.W);
        }


        /// <summary>
        /// Convert an RGB color to HSL
        /// </summary>
        /// <param name="color">the RGB color to convert</param>
        /// <returns>The HSLA representation of the color</returns>
        public static Vector4 ColorToHSL(this Color color)
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

            return new Vector4(h, s, L, color.A / 255f);
        }

        public static Color ColorFromHSL(Vector4 hsla)
        {
            return ColorFromHSL(hsla.X, hsla.Y, hsla.Z, hsla.W);
        }

        public static Color ColorFromHSL(float hue, float saturation, float lightness, float alpha = 1)
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
    }
}