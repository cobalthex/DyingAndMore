using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    /// <summary>
    /// A simple camera that can either track its own position or follow an entity
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// The map this camera is tracking
        /// </summary>
        public Map Map { get; set; }

        /// <summary>
        /// An optional post effect to apply to this camera
        /// </summary>
        public Effect PostEffect { get; set; } = null;

        /// <summary>
        /// Where to draw this camera to
        /// </summary>
        public Rectangle Viewport { get; set; }

        protected Vector2 lastPosition = Vector2.Zero;

        /// <summary>
        /// How quickly to pan the camera between it's old and new positions
        /// </summary>
        public float MoveSpeed = 1000;

        /// <summary>
        /// The current position of the camera
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// An offset to the camera position
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;

        /// <summary>
        /// An entity to follow
        /// </summary>
        public Entity Follow { get; set; } = null;

        public Camera(Map Map, Entity Follow = null)
        {
            this.Map = Map;
            this.Follow = Follow;
        }

        /// <summary>
        /// Update the camera (and map)
        /// </summary>
        /// <param name="Time">Game time</param>
        public void Update(GameTime Time)
        {
            if (Follow != null)
                Position = Follow.Position;

            /* linear interpolation
            var deltaS = (float)Time.ElapsedGameTime.TotalSeconds * MoveSpeed;
            var dist = Vector2.Distance(lastPosition, Position);
            if (dist <= deltaS)
                lastPosition = Position;
            else
                lastPosition = Vector2.Lerp(lastPosition, Position, (1 / dist) * deltaS);
            */
            lastPosition = Vector2.Lerp(lastPosition, Position, (float)Time.ElapsedGameTime.TotalSeconds * (MoveSpeed / 100));

            Map.Update(Time, lastPosition + Offset, Viewport);
        }

        /// <summary>
        /// Draw the map at the camera's position
        /// </summary>
        public void Draw()
        {
            Draw(Viewport);
        }

        /// <summary>
        /// Draw the map at the camera's position
        /// </summary>
        /// <param name="Viewport">An explicit viewport to draw to</param>
        public void Draw(Rectangle Viewport)
        {
            var cam = lastPosition + Offset;
            Map.Draw(new Vector2(cam.X - (Viewport.Width / 2), cam.Y - (Viewport.Height / 2)), Viewport, PostEffect);
        }

        /// <summary>
        /// Calculate the base world position for a camera's view
        /// </summary>
        /// <param name="Camera">The position of the camera</param>
        /// <param name="Viewport">The viewport rectangle, centered around the camera</param>
        /// <returns>The world position of the camera</returns>
        public Vector2 GetViewStart(Vector2 Camera, Rectangle Viewport)
        {
            return new Vector2(Viewport.X - (int)Camera.X - (Viewport.Width / 2), Viewport.Y - (int)Camera.X - (Viewport.Width / 2));
        }
    }
}
