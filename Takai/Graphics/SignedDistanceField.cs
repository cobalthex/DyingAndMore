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

        //todo: OnEdge()

        //public static byte[] Generate(Color[] texmap, int width, int height, int scale)
        //{
            //    var timer = new System.Diagnostics.Stopwatch();
            //    timer.Start();

            //    var w = width >> scale;
            //    var h = height >> scale;

            //    if (w <= 0 || h <= 0)
            //        return new byte[0];

            //    //use floats?

            //    //1px border around map is considered unpathable

            //    //do each individually for better cache perf

            //    var sdf = new byte[h * w];
            //    for (int y = 1; y < h - 1; ++y)
            //        for (int x = 1; x < w - 1; ++x)
            //            sdf[y * w + x] = 255;

            //    var p = new Point[h * w];
            //    for (int y = 1; y < h - 1; ++y)
            //        for (int x = 1; x < w - 1; ++x)
            //            p[y * w + x] = new Point(-1);


            //    //can skip bounds checking by using padded array, but requires copying array at end

            ////find edges
            ////can be parallelized
            //const byte threshold = 127;
            //for (int y = 1; y < h - 1; ++y)
            //{
            //    var ys = y << scale;
            //    for (int x = 1; x < w - 1; ++x)
            //    {
            //        var xs = x << scale;
            //        byte cur = TileValueAt(xs, ys, tilemap);
            //        //from 'Fast Edge Detection Algorithm for Embedded Systems'
            //        bool isEdge =
            //            (cur - TileValueAt((x + 1) << scale, ys, tilemap) > threshold) ||
            //            (cur - TileValueAt(xs, (y + 1) << scale, tilemap) > threshold);
            //        if (cur < threshold || isEdge)
            //        {
            //            sdf[y * w + x] = 0;
            //            p[y * w + x] = new Point(x, y);
            //        }

            //    }
            //}
            //System.Diagnostics.Debug.WriteLine("Edge pass SDF in " + timer.Elapsed);

            ////first pass
            //for (int y = 1; y < h - 1; ++y)
            //{
            //    for (int x = 1; x < w - 1; ++x)
            //    {
            //        var cc = y * w + x;

            //        var cp = (y - 1) * w + (x - 1);
            //        if (sdf[cp] + DHyp < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //        cp = (y - 1) * w + x;
            //        if (sdf[cp] + DY < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //        cp = (y - 1) * w + (x + 1);
            //        if (sdf[cp] + DHyp < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //        cp = y * w + (x - 1);
            //        if (sdf[cp] + DX < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //    }
            //}

            ////second, and final pass
            //for (int y = h - 2; y > 0; --y)
            //{
            //    for (int x = w - 2; x > 0; --x)
            //    {
            //        var cc = y * w + x;

            //        var cp = y * w + (x + 1);
            //        if (sdf[cp] + DX < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //        cp = (y + 1) * w + (x - 1);
            //        if (sdf[cp] + DHyp < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //        cp = (y + 1) * w + x;
            //        if (sdf[cp] + DY < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //        cp = (y + 1) * w + (x + 1);
            //        if (sdf[cp] + DX < sdf[cc])
            //        {
            //            p[cc] = p[cp];
            //            sdf[cc] = Euclidean(x - p[cc].X, y - p[cc].Y);
            //        }
            //    }
            //}
            //    return sdf;
            //}
        }
    }
