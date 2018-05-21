using System;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// The base class for AI or player controllers
    /// </summary>
    public abstract class Controller : ICollisionResolver
    {
        [Takai.Data.Serializer.Ignored]
        public virtual ActorInstance Actor { get;
            set; }

        public virtual Controller Clone()
        {
            var clone = (Controller)MemberwiseClone();
            clone.Actor = null;
            return clone;
        }

        /// <summary>
        /// One frame of time to control the actor
        /// </summary>
        /// <param name="deltaTime">How long since the last Think cycle</param>
        public abstract void Think(System.TimeSpan deltaTime);

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

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
