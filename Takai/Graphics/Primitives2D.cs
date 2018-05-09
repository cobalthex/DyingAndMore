using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Graphics
{
    public static class Primitives2D
    {
        private static Texture2D _pixel;

        private static void Init(GraphicsDevice device)
        {
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData<Color>(new[] { Color.White });
        }

        /// <summary>
        /// Draw one or more lines (one pixel width)
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use</param>
        /// <param name="color">The color of the line</param>
        /// <param name="points">The verticies of the line, if there are less than 2 points, no line is drawn</param>
        public static void DrawLine(SpriteBatch spriteBatch, Color color, params Vector2[] points)
        {
            if (_pixel == null)
                Init(spriteBatch.GraphicsDevice);

            for (int i = 0; i < points.Length - 1; ++i)
            {
                float angle = (float)System.Math.Atan2(points[i + 1].Y - points[i].Y, points[i + 1].X - points[i].X);
                float length = (points[i + 1] - points[i]).Length();

                spriteBatch.Draw(_pixel, points[i], null, color, angle, Vector2.Zero, new Vector2(length, 1), SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// Draw a single pixel dot
        /// </summary>
        /// <param name="spriteBatch">The Spritebatch to use</param>
        /// <param name="color">The color of the dot</param>
        /// <param name="point">The point to draw the dot at</param>
        public static void DrawDot(SpriteBatch spriteBatch, Color color, Vector2 point)
        {
            if (_pixel == null)
                Init(spriteBatch.GraphicsDevice);

            spriteBatch.Draw(_pixel, point, color);
        }

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use</param>
        /// <param name="color">The color of the lines</param>
        /// <param name="rectangle">The rectangle to draw</param>
        public static void DrawRect(SpriteBatch spriteBatch, Color color, Rectangle rectangle)
        {
            DrawLine(spriteBatch, color, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.X + rectangle.Width, rectangle.Y),
                new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height), new Vector2(rectangle.X, rectangle.Y + rectangle.Height), new Vector2(rectangle.X, rectangle.Y));
        }

        /// <summary>
        /// Draw a filled rectangle
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use</param>
        /// <param name="color">The color of the region</param>
        /// <param name="rectangle">The rectangle of the filled region</param>
        public static void DrawFill(SpriteBatch spriteBatch, Color color, Rectangle rectangle)
        {
            if (_pixel == null)
                Init(spriteBatch.GraphicsDevice);

            spriteBatch.Draw(_pixel, rectangle, color);
        }

        public static void DrawX(SpriteBatch spriteBatch, Color color, Rectangle bounds)
        {
            var a = bounds.Location.ToVector2();
            var b = a + new Vector2(bounds.Width, bounds.Height);
            DrawLine(spriteBatch, color, a, b);

            var x = a.X;
            a.X = b.X;
            b.X = x;
            DrawLine(spriteBatch, color, a, b);
        }
    }
}
