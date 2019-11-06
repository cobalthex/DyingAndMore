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
                var diff = value - _position;
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
        public IReadOnlyCollection<EntityInstance> WorldChildren => _worldChildren?.AsReadOnly();
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

        void UpdateWorldState()
        {
            localTransform = new Matrix(Forward.X, -Forward.Y, 0, 0, Forward.Y, Forward.X, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            localTransform *= Matrix.CreateTranslation(_position.X, _position.Y, 0);

            Transform = localTransform;
            if (WorldParent != null)
                Transform *= WorldParent.Transform;

            if (WorldChildren != null)
            {
                //stack based impl?
                foreach (var child in WorldChildren)
                    UpdateWorldState();
            }
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

            var v = Vector2.Transform(new Vector2(rmin.X, rmin.Y), localTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(rmax.X, rmin.Y), localTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(rmax.X, rmax.Y), localTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(rmin.X, rmax.Y), localTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            var size = max - min;
            var r = new Rectangle(0, 0, (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));
            r.Offset((int)min.X, (int)min.Y);

            //children should be relative to rotation
            //if (WorldChildren != null)
            //{
            //    foreach (var child in WorldChildren)
            //        r = Rectangle.Intersect(r, child.AxisAlignedBounds);
            //}

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
