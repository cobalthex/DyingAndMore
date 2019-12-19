using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    //child objects do not currently have physics,
    //aabb and transforms are updated however

    public partial class EntityInstance
    {
        /// <summary>
        /// The current position of the entity, relative to <see cref="WorldParent"/>, or world if none
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateWorldState();
            }
        }
        private Vector2 _position;

        /// <summary>
        /// The (normalized) direction the entity is facing
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Forward
        {
            get => _forward;
            set
            {
                _forward = value;
                UpdateWorldState();
            }
        }
        private Vector2 _forward = Vector2.UnitX;

        /// <summary>
        /// The velocity of the entity, separate from <see cref="Forward"/>
        /// Ignored if <see cref="WorldParent"/> is not null
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        /// <summary>
        /// The parent of this element for physics/collision
        /// </summary>
        public EntityInstance WorldParent { get; internal set; } = null;

        /// <summary>
        /// Any entities attached to this one for physics/collisions
        /// May be null
        /// </summary>
        public IReadOnlyList<EntityInstance> WorldChildren => _worldChildren?.AsReadOnly();
        internal List<EntityInstance> _worldChildren = null;

        [Data.Serializer.Ignored]
        public float Radius { get; private set; }
        [Data.Serializer.Ignored]
        public float RadiusSq => Radius * Radius; //todo: cache

        /// <summary>
        /// the axis aligned bounding box of this entity and children
        /// updated whenever the state, position, or direction changes
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle AxisAlignedBounds { get; private set; }

        internal Rectangle lastAABB; //used for tracking movement in spacial grid

        internal Matrix localTransform = Matrix.Identity;

        public Matrix Transform { get; internal set; } = Matrix.Identity;

        /// <summary>
        /// Position, with all parent transforms applied
        /// </summary>
        public Vector2 RealPosition => new Vector2(Transform.M41, Transform.M42);
        /// <summary>
        /// Forward, with all parent transforms applied
        /// </summary>
        public Vector2 RealForward => new Vector2(Transform.M11, Transform.M12);

        /// <summary>
        /// Set the local position to an already transformed point
        /// </summary>
        /// <param name="transformedPosition">The transformed (world space) position</param>
        /// <returns>The new, local position</returns>
        public Vector2 SetPositionTransformed(Vector2 transformedPosition)
        {
            //todo: merge with RealPosition?
            if (WorldParent == null)
                Position = transformedPosition;
            else
                Position = Vector2.Transform(transformedPosition, Matrix.Invert(WorldParent.Transform));
            return Position;
        }
        /// <summary>
        /// Set the local forward to an already transformed direction
        /// </summary>
        /// <param name="transformedForward">The transformed (world space) forward (does not need to be normalized)</param>
        /// <returns>The new, local forward</returns>
        public Vector2 SetForwardTransformed(Vector2 transformedForward)
        {
            transformedForward.Normalize();
            //todo: merge with RealForward?
            if (WorldParent == null)
                Forward = transformedForward;
            else
                Forward = Vector2.TransformNormal(transformedForward, Matrix.Invert(WorldParent.Transform));
            return Forward;
        }

        internal void UpdateWorldState()
        {
            var rot = new Matrix( //flip Y
                 Forward.X, Forward.Y, 0, 0,
                -Forward.Y, Forward.X, 0, 0,
                0,          0,         1, 0, 
                0,          0,         0, 1
            );
            var trans = new Matrix(
                1,          0,          0, 0,
                0,          1,          0, 0,
                0,          0,          1, 0,
                Position.X, Position.Y, 0, 1
            );

            //todo: if applying scale, need to update code to apply scale to radius/etc

            Transform = rot * trans;
            if (WorldParent != null)
                Transform *= WorldParent.Transform;

            if (WorldChildren != null)
            {
                foreach (var child in WorldChildren)
                    child.UpdateWorldState();
            }

            UpdateAxisAlignedBounds();
        }

        internal void UpdateAxisAlignedBounds()
        {
            //todo: use colliders & calculate from there

            var rmin = new Vector2(-Radius);
            var rmax = new Vector2(Radius);

            //todo: handle origin
            //todo: needs to be on per-animation basis (perhaps sizeRotated, sizeFixed)

            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);

            var v = Vector2.Transform(new Vector2(rmin.X, rmin.Y), Transform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(rmax.X, rmin.Y), Transform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(rmax.X, rmax.Y), Transform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(rmin.X, rmax.Y), Transform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            var size = max - min;
            var r = new Rectangle(0, 0, (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));
            r.Offset((int)min.X, (int)min.Y);

            AxisAlignedBounds = r;
        }

        public float ForwardSpeed()
        {
            return Vector2.Dot(Forward, Velocity);
        }

        /// <summary>
        /// Called when there is a collision between this instance and another
        /// </summary>
        /// <param name="collider">The instance collided with</param>
        /// <param name="deltaTime">How long since the last frame (in map time)</param>
        public virtual void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime) { }

        /// <summary>
        /// Called when there is a collision between this entity and the map
        /// </summary>
        /// <param name="tile">The tile on the map where the collision occurred</param>
        /// <param name="deltaTime">How long since the last frame (in map time)</param>
        public virtual void OnMapCollision(Point tile, Vector2 point, TimeSpan deltaTime) { }

        /// <summary>
        /// Called when there is a collision between this entity and a Fluid
        /// </summary>
        /// <param name="fluid">The type of Fluid collided with</param>
        /// <param name="deltaTime">How long since the last frame (in map time)</param>
        public virtual void OnFluidCollision(FluidClass fluid, TimeSpan deltaTime) { }
    }
}
