using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Takai.Graphics
{
    public static class SignedDistanceField
    {
        //distance between pixels

        public const float DX = 1f;
        public const float DY = 1f;
        public const float DHyp = 1.4142135623730950488f;

        //delegates? 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Euclidean(int x, int y) => (byte)Math.Min(255, Math.Sqrt(x * x + y * y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Manhattan(int x, int y) => (byte)Math.Min(255, (Math.Abs(x) + Math.Abs(y)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Chebyshev(int x, int y) => (byte)Math.Min(255, Math.Max(Math.Abs(x), Math.Abs(y)));

        public static byte[] Generate(Microsoft.Xna.Framework.Graphics.Texture2D texture, int downscale = 0)
        {
            Color[] texmap = new Color[texture.Width * texture.Height];
            texture.GetData(texmap);
            return Generate(texmap, texture.Width, texture.Height, downscale);
        }

        /// <summary>
        /// Generate an SDF from a texture
        /// </summary>
        /// <param name="texmap">The image data to use</param>
        /// <param name="width">Width of the <see cref="texmap"/></param>
        /// <param name="height">Height of the <see cref="texmap"/></param>
        /// <param name="downscale">scale reduction factor when generating the SDF (in the format of Width/Height >> scale)</param>
        /// <returns>The generated SDF</returns>
        public static byte[] Generate(Color[] texmap, int width, int height, int downscale = 0)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            var w = width >> downscale;
            var h = height >> downscale;

            if (w <= 0 || h <= 0)
                return new byte[0];

            //use floats?

            //1px border around map is considered unpathable

            //do each individually for better cache perf

            var sdf = new byte[h * w];
            for (int y = 1; y < h - 1; ++y)
                for (int x = 1; x < w - 1; ++x)
                    sdf[y * w + x] = 255;

            var p = new Point[h * w];
            for (int y = 1; y < h - 1; ++y)
                for (int x = 1; x < w - 1; ++x)
                    p[y * w + x] = new Point(-1);


            //can skip bounds checking by using padded array, but requires copying array at end

            //find edges
            //can be parallelized
            const byte threshold = 127;
            for (int y = 0; y < h - 1; ++y)
            {
                var ys = y << downscale;
                for (int x = 0; x < w - 1; ++x)
                {
                    var xs = x << downscale;
                    var cur = texmap[ys * width + xs].A;
                    //from 'Fast Edge Detection Algorithm for Embedded Systems'
                    bool isEdge =
                        (cur - texmap[ys * width + ((x + 1) << downscale)].A > threshold) ||
                        (cur - texmap[((y + 1) << downscale) * width + xs].A > threshold);
                    if (cur < threshold || isEdge)
                    {
                        sdf[y * w + x] = 0;
                        p[y * w + x] = new Point(x, y);
                    }

                }
            }
            System.Diagnostics.Debug.WriteLine("Edge pass SDF in " + timer.Elapsed);

            //this pass and edge pass can be split into separate functions.
            //tilemap sdf can share the following code

            //first pass
            for (int y = 1; y < h - 1; ++y)
            {
                for (int x = 1; x < w - 1; ++x)
                {
                    var cc = y * w + x;

                    var cp = (y - 1) * w + (x - 1);
                    if (sdf[cp] + DHyp < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y - 1) * w + x;
                    if (sdf[cp] + DY < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y - 1) * w + (x + 1);
                    if (sdf[cp] + DHyp < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = y * w + (x - 1);
                    if (sdf[cp] + DX < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                }
            }

            //second, and final pass
            for (int y = h - 2; y > 0; --y)
            {
                for (int x = w - 2; x > 0; --x)
                {
                    var cc = y * w + x;

                    var cp = y * w + (x + 1);
                    if (sdf[cp] + DX < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y + 1) * w + (x - 1);
                    if (sdf[cp] + DHyp < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y + 1) * w + x;
                    if (sdf[cp] + DY < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y + 1) * w + (x + 1);
                    if (sdf[cp] + DX < sdf[cc])
                    {
                        p[cc] = p[cp];
                        sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                }
            }
            return sdf;
        }

        public static void SaveToTGA(byte[] sdf, short width, short height, System.IO.Stream stream)
        {
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(3);
            var bytes = new byte[5 + 4];
            stream.Write(bytes, 0, bytes.Length);

            bytes = BitConverter.GetBytes(width);
            stream.Write(bytes, 0, bytes.Length);
            bytes = BitConverter.GetBytes(height);
            stream.Write(bytes, 0, bytes.Length);

            stream.WriteByte(8); //bpp
            stream.WriteByte(0);

            for (var y = height - 1; y >= 0; --y) //y is flipped
                stream.Write(sdf, y * width, width);
        }
    }
}
