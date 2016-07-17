using System;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// The basic entity. All actors and objects inherit from this
    /// </summary>
    public class Entity : ICloneable
    {
        /// <summary>
        /// The name of this entity. Typically used by other entities for locating
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The current position of the entity
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// The (normalized) direction the entity is facing
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Direction { get; set; } = Vector2.UnitX;
        /// <summary>
        /// The direction the entity is moving
        /// </summary>
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        /// <summary>
        /// The map the entity is in, null if none
        /// </summary>
        [Data.NonSerialized]
        public Map Map { get; internal set; } = null;
        /// <summary>
        /// The section the entity is located in in the map, null if none or if in the active set
        /// </summary>
        [Data.NonSerialized]
        public MapSector Sector { get; internal set; } = null;

        /// <summary>
        /// Determines if the entity is thinking (alive)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Determines if this entity is always part of the active set and therefore always updated
        /// </summary>
        /// <remarks>This is typically used for things like projectiles</remarks>
        public bool AlwaysActive { get; set; } = false;

        /// <summary>
        /// The radius of this entity. Used mainly for broad-phase collision
        /// </summary>
        public float Radius
        {
            get
            {
                return radius;
            }
            set
            {
                radius = value;
                RadiusSq = value * value;
            }
        }
        [Data.NonSerialized]
        public float RadiusSq { get; private set; }
        private float radius = 1;

        /// <summary>
        /// On collision, should this entity try to 'uncollide' with the entity
        /// </summary>
        /// <example>Triggers would have this set to false</example>
        public bool IsPhysical { get; set; } = true;

        /// <summary>
        /// Trace (raycast) queries should ignore this entity
        /// </summary>
        public bool IgnoreTrace { get; set; } = false;

        /// <summary>
        /// The sprite for this entity (may be null for things like triggers)
        /// Can be updated by components
        /// </summary>
        public Graphics.Graphic Sprite { get; set; } = null;

        /// <summary>
        /// Draw an outline around the sprite. If A is 0, ignored
        /// </summary>
        public Color OutlineColor { get; set; } = Color.Transparent;

        public Entity() { }

        /// <summary>
        /// Clone this entity
        /// </summary>
        /// <returns>A cloned entity</returns>
        /// <remarks>Map information is not cloned</remarks>
        public virtual object Clone()
        {
            var cloned = (Entity)MemberwiseClone();

            cloned.Map = null;
            cloned.Sector = null;

            return cloned;
        }

        /// <summary>
        /// Called when the entity is loaded. Loads all components
        /// </summary>
        public virtual void Load() { }
        /// <summary>
        /// Called when the entity is unloaded. Unloads all components
        /// </summary>
        public virtual void Unload() { }

        /// <summary>
        /// The basic think function for this entity, called once a frame
        /// </summary>
        /// <param name="Time">Frame time</param>
        public virtual void Think(GameTime Time) { }

        /// <summary>
        /// Called when there is a collision between this entity and another entity
        /// </summary>
        /// <param name="Collider">The entity collided with</param>
        public virtual void OnEntityCollision(Entity Collider, Vector2 Point) { }

        /// <summary>
        /// Called when there is a collision between this entity and the map
        /// </summary>
        /// <param name="Tile">The tile on the map where the collision occurred</param>
        public virtual void OnMapCollision(Point Tile, Vector2 Point) { }

        /// <summary>
        /// Called when there is a collision between this entity and a blob
        /// </summary>
        /// <param name="Type">The type of blob collided with</param>
        public virtual void OnBlobCollision(BlobType Type) { }
    }
}
