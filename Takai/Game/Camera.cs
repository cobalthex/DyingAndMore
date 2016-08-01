using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    /// <summary>
    /// A simple camera that can either track its own position or follow an entity
    /// </summary>
    [Data.DesignerCreatable]
    public class Camera
    {
        /// <summary>
        /// The map this camera is tracking
        /// </summary>
        [Data.NonDesigned]
        public Map Map { get; set; }

        /// <summary>
        /// An optional post effect to apply to this camera
        /// </summary>
        public Effect PostEffect { get; set; } = null;

        /// <summary>
        /// Where to draw this camera to
        /// </summary>
        public Rectangle Viewport { get; set; }

        /// <summary>
        /// The current position of the camera, used for smooth movement
        /// Moves towards <see cref="Position"/>
        /// </summary>
        [Data.NonSerialized]
        public Vector2 ActualPosition { get; protected set; } = Vector2.Zero;

        /// <summary>
        /// How quickly to pan the camera between it's old and new positions
        /// </summary>
        public float MoveSpeed = 1000;

        /// <summary>
        /// The target position of the camera
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// An offset to the camera position
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;


        /// <summary>
        /// An entity to follow
        /// </summary>
        [Data.NonDesigned]
        public Entity Follow { get; set; } = null;

        public Camera() { }

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
            var dist = Vector2.Distance(LastPosition, Position);
            if (dist <= deltaS)
                LastPosition = Position;
            else
                LastPosition = Vector2.Lerp(LastPosition, Position, (1 / dist) * deltaS);
            */
            ActualPosition = Vector2.Lerp(ActualPosition, Position, (float)Time.ElapsedGameTime.TotalSeconds * (MoveSpeed / 100));

            Map.Update(Time, ActualPosition + Offset, Viewport);
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
            var cam = ActualPosition + Offset;
            Map.Draw(cam - GetCameraOrigin(Viewport.Width, Viewport.Height), Viewport, PostEffect);
        }

        /// <summary>
        /// Teleport the camera to a position
        /// </summary>
        /// <param name="Position">The position to move to</param>
        public void MoveTo(Vector2 Position)
        {
            ActualPosition = this.Position = Position;
        }


        /// <summary>
        /// Get the camera origin point in a specified region
        /// </summary>
        /// <param name="ViewWidth">The viewport width</param>
        /// <param name="ViewHeight">The viewport height</param>
        /// <returns>The origin point for the camera</returns>
        public static Vector2 GetCameraOrigin(int ViewWidth, int ViewHeight)
        {
            return new Vector2(ViewWidth / 2, ViewHeight / 2);
        }

        /// <summary>
        /// Get the camera origin point in a rectangle
        /// </summary>
        /// <param name="Viewport">The viewport</param>
        /// <returns>The origin point for the camera</returns>
        public static Vector2 GetCameraOrigin(Rectangle Viewport)
        {
            return new Vector2(Viewport.X - (Viewport.Width / 2), Viewport.Y - (Viewport.Height / 2));
        }

        /// <summary>
        /// Unproject a world position to a screen position
        /// </summary>
        /// <param name="WorldPosition">The world position to convert</param>
        /// <returns>The screen position of a world position</returns>
        public Vector2 WorldToScreen(Vector2 WorldPosition)
        {
            return WorldToScreen(WorldPosition, Viewport);
        }

        /// <summary>
        /// Unproject a world position to a screen position 
        /// </summary>
        /// <param name="WorldPosition">The world position to convert</param>
        /// <param name="Viewport">The viewport rectangle</param>
        /// <returns>The screen position of a world position</returns>
        public static Vector2 WorldToScreen(Vector2 WorldPosition, Rectangle Viewport)
        {
            return new Vector2(Viewport.X, Viewport.Y) - (WorldPosition - GetCameraOrigin(Viewport.Width, Viewport.Height));
        }

        /// <summary>
        /// Unproject a world position to a screen position (rectangle form)
        /// </summary>
        /// <param name="WorldRect">The world position rectangle to convert</param>
        /// <param name="Viewport">The viewport rectangle</param>
        /// <returns>The screen position of a world position</returns>
        public static Rectangle WorldToScreen(Rectangle WorldRect, Rectangle Viewport)
        {
            var origin = GetCameraOrigin(Viewport.Width, Viewport.Height).ToPoint();
            return new Rectangle(Viewport.X - (WorldRect.X - origin.X), Viewport.Y - (WorldRect.Y - origin.Y), WorldRect.Width, WorldRect.Height);
        }

        /// <summary>
        /// Project a screen position to a world position using the current camera's viewport
        /// </summary>
        /// <param name="ScreenPosition">The screen position</param>
        /// <returns>The world position</returns>
        public Vector2 ScreenToWorld(Vector2 ScreenPosition)
        {
            return ScreenToWorld(ScreenPosition, ActualPosition, Viewport);
        }

        /// <summary>
        /// Project a screen position to a world position using the current camera's viewport
        /// </summary>
        /// <param name="ScreenPosition">The screen position</param>
        /// <param name="CameraPosition">The camera's position in the world</param>
        /// <param name="Viewport">The camera's viewport</param>
        /// <returns>The world position</returns>
        public static Vector2 ScreenToWorld(Vector2 ScreenPosition, Vector2 CameraPosition, Rectangle Viewport)
        {
            return GetCameraOrigin(Viewport) + CameraPosition + ScreenPosition;
        }

        /// <summary>
        /// Is a world position visible on screen? (Based on the camera's position and viewport)
        /// </summary>
        /// <param name="Point">The world position to test</param>
        /// <returns>True if the position is inside the screen boundaries</returns>
        public bool IsVisible(Vector2 WorldPosition)
        {
            return IsVisible(new Rectangle((int)WorldPosition.X, (int)WorldPosition.Y, 1, 1), ActualPosition, Viewport);
        }

        /// <summary>
        /// Is a world position and boundary visible on screen? (Based on the camera's position and viewport)
        /// </summary>
        /// <param name="WorldRect">The boundaries to check</param>
        /// <returns>True if the rectangle intersects the screen boundaries</returns>
        public bool IsVisible(Rectangle WorldRect)
        {
            return IsVisible(WorldRect, ActualPosition, Viewport);
        }

        /// <summary>
        /// Is a world position and boundary visible on screen? (Based on the camera's position and viewport)
        /// </summary>
        /// <param name="WorldRect">The boundaries to check</param>
        /// <param name="CameraPosition">The position of the camera</param>
        /// <param name="Viewport">The screen boundaries</param>
        /// <returns>True if the rectangle intersects the screen boundaries</returns>
        public static bool IsVisible(Rectangle WorldRect, Vector2 CameraPosition, Rectangle Viewport)
        {
            var w2s = WorldToScreen(WorldRect, Viewport);
            Viewport.Offset(-CameraPosition);
            return Viewport.Intersects(w2s);
        }
    }
}
