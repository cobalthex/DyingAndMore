using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Graphics
{
    public static class Primitives2D
    {
        private static Texture2D _pixel;

        private static void Init(GraphicsDevice GDev)
        {
            _pixel = new Texture2D(GDev, 1, 1);
            _pixel.SetData<Color>(new[] { Color.White });
        }

        /// <summary>
        /// Draw one or more lines (one pixel width)
        /// </summary>
        /// <param name="SpriteBatch">The sprite batch to use</param>
        /// <param name="Color">The color of the line</param>
        /// <param name="Points">The verticies of the line, if there are less than 2 points, no line is drawn</param>
        public static void DrawLine(SpriteBatch SpriteBatch, Color Color, params Vector2[] Points)
        {
            if (_pixel == null)
                Init(SpriteBatch.GraphicsDevice);

            for (int i = 0; i < Points.Length - 1; i++)
            {
                float angle = (float)System.Math.Atan2(Points[i + 1].Y - Points[i].Y, Points[i + 1].X - Points[i].X);
                float length = (Points[i + 1] - Points[i]).Length();

                SpriteBatch.Draw(_pixel, Points[i], null, Color, angle, Vector2.Zero, new Vector2(length, 1), SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// Draw a single pixel dot
        /// </summary>
        /// <param name="SpriteBatch">The Spritebatch to use</param>
        /// <param name="Color">The color of the dot</param>
        /// <param name="Point">The point to draw the dot at</param>
        public static void DrawDot(SpriteBatch SpriteBatch, Color Color, Vector2 Point)
        {
            if (_pixel == null)
                Init(SpriteBatch.GraphicsDevice);

            SpriteBatch.Draw(_pixel, Point, Color);
        }

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        /// <param name="SpriteBatch">The sprite batch to use</param>
        /// <param name="Color">The color of the lines</param>
        /// <param name="Rectangle">The rectangle to draw</param>
        public static void DrawRect(SpriteBatch SpriteBatch, Color Color, Rectangle Rectangle)
        {
            DrawLine(SpriteBatch, Color, new Vector2(Rectangle.X, Rectangle.Y), new Vector2(Rectangle.X + Rectangle.Width, Rectangle.Y),
                new Vector2(Rectangle.X + Rectangle.Width, Rectangle.Y + Rectangle.Height), new Vector2(Rectangle.X, Rectangle.Y + Rectangle.Height), new Vector2(Rectangle.X, Rectangle.Y));
        }

        /// <summary>
        /// Draw a filled rectangle
        /// </summary>
        /// <param name="SpriteBatch">The sprite batch to use</param>
        /// <param name="Color">The color of the region</param>
        /// <param name="Rectangle">The rectangle of the filled region</param>
        public static void DrawFill(SpriteBatch SpriteBatch, Color Color, Rectangle Rectangle)
        {
            if (_pixel == null)
                Init(SpriteBatch.GraphicsDevice);

            SpriteBatch.Draw(_pixel, Rectangle, Color);
        }
    }
}
