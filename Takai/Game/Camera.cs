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
        [Data.Serializer.Ignored]
        public Vector2 ActualPosition { get; protected set; } = Vector2.Zero;

        /// <summary>
        /// How quickly to pan the camera between it's old and new positions
        /// </summary>
        public float MoveSpeed = 1000;

        /// <summary>
        /// The target position of the camera
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        public float Scale { get; set; } = 1;

        public float Rotation { get; set; } = 0;

        /// <summary>
        /// The actual rotation, which is rotation + shake (randomized)
        /// </summary>
        private float actualRotation = 0;

        /// <summary>
        /// How much the camera should shake every frame (random amount up to this value) in radians
        /// 0 for none
        /// </summary>
        public float Shake
        {
            get { return shake; }
            set {
                shake = value;
                actualRotation = Rotation;
            }
        }
        private float shake = 0;

        /// <summary>
        /// The visible (AABB) region of the map that this camera encompasses
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle VisibleRegion
        {
            get
            {
                // a - b
                // |   |
                // c - d

                var centerTransform = new Vector2(Viewport.Width / 2, Viewport.Height / 2);
                var transform = Matrix.Invert(
                    Matrix.CreateTranslation(-ActualPosition.X, -ActualPosition.Y, 0) *
                    Matrix.CreateRotationZ(actualRotation) *
                    Matrix.CreateScale(Scale)
                );

                var a = Vector2.Transform(new Vector2(-centerTransform.X, -centerTransform.Y), transform);
                var b = Vector2.Transform(new Vector2( centerTransform.X, -centerTransform.Y), transform);
                var c = Vector2.Transform(new Vector2(-centerTransform.X,  centerTransform.Y), transform);
                var d = Vector2.Transform(new Vector2( centerTransform.X,  centerTransform.Y), transform);

                Vector2 min = Vector2.Min(Vector2.Min(Vector2.Min(a, b), c), d);
                Vector2 max = Vector2.Max(Vector2.Max(Vector2.Max(a, b), c), d);

                var size = max - min;
                return new Rectangle(min.ToPoint(), new Point((int)System.Math.Ceiling(size.X), (int)System.Math.Ceiling(size.Y)));
            }
        }

        //todo: camera viewport, transformed

        /// <summary>
        /// An entity to follow
        /// </summary>
        public EntityInstance Follow { get; set; } = null;

        /// <summary>
        /// A transformation matrix passed to the map renderer
        /// Calculated from Position, offset, Zoom, and Angle
        /// </summary>
        /// <remarks>Setting transform will set position, scale and angle; and offset to zero</remarks>
        [Data.Serializer.Ignored]
        public Matrix Transform
        {
            get
            {
                return Matrix.CreateTranslation(-ActualPosition.X, -ActualPosition.Y, 0) *
                       Matrix.CreateRotationZ(actualRotation) *
                       Matrix.CreateScale(Scale) *
                       Matrix.CreateTranslation(new Vector3(GetCameraOrigin(Viewport.Width, Viewport.Height), 0));
            }
        }

        private System.Random randGen = new System.Random();

        public Camera() { }

        public Camera(EntityInstance follow = null)
        {
            Follow = follow;
            if (Follow != null)
                ActualPosition = Position = Follow.Position;
        }

        public Camera(Vector2 position)
        {
            Follow = null;
            ActualPosition = Position = position;
        }

        public void _DebugSetTransform(bool relative, Vector2 position)
        {
            if (relative)
            {
                ActualPosition += position;
            }
            else
            {
                ActualPosition = position;
            }
        }

        /// <summary>
        /// Update the camera (and map)
        /// </summary>
        /// <param name="time">Game time</param>
        public virtual void Update(GameTime time)
        {
            if (Follow != null)
                Position = Follow.Position;

            if (Shake != 0)
                actualRotation = Rotation + ((float)randGen.NextDouble() * 2 * Shake - Shake);

            /* linear interpolation
            var deltaS = (float)Time.ElapsedGameTime.TotalSeconds * MoveSpeed;
            var dist = Vector2.Distance(LastPosition, Position);
            if (dist <= deltaS)
                LastPosition = Position;
            else
                LastPosition = Vector2.Lerp(LastPosition, Position, (1 / dist) * deltaS);
            */
            actualRotation = MathHelper.Lerp(actualRotation, Rotation, (float)time.ElapsedGameTime.TotalSeconds * (MoveSpeed / 100));
            ActualPosition = Vector2.Lerp(ActualPosition, Position, (float)time.ElapsedGameTime.TotalSeconds * (MoveSpeed / 100));
        }

        /// <summary>
        /// Teleport the camera to a position
        /// </summary>
        /// <param name="position">The position to move to</param>
        public void MoveTo(Vector2 position)
        {
            ActualPosition = this.Position = position;
        }


        /// <summary>
        /// Get the camera origin point in a specified region
        /// </summary>
        /// <param name="viewWidth">The viewport width (transformed)</param>
        /// <param name="viewHeight">The viewport height (transformed)</param>
        /// <returns>The origin point for the camera</returns>
        public static Vector2 GetCameraOrigin(float viewWidth, float viewHeight)
        {
            return new Vector2(viewWidth / 2, viewHeight / 2);
        }

        /// <summary>
        /// Unproject a world position to a screen position
        /// </summary>
        /// <param name="worldPosition">The world position to convert</param>
        /// <returns>The screen position of a world position</returns>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, Transform);
        }

        /// <summary>
        /// Project a screen position to a world position using the current camera's viewport
        /// </summary>
        /// <param name="screenPosition">The screen position</param>
        /// <returns>The world position</returns>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(Transform));
        }
    }
}
