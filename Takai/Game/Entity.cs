using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// All of the possible entity states
    /// </summary>
    public enum EntStateKey
    {
        Invalid,
        Dead,
        Idle, 
        Dying,
        Inactive,
        Active,
    }

    public class EntState : Takai.IState
    {
        public Takai.Graphics.Sprite Sprite { get; set; }

        public bool IsLooping
        {
            get
            {
                return Sprite.IsLooping;
            }
            set
            {
                Sprite.IsLooping = value;
            }
        }

        public bool HasFinished()
        {
            return Sprite.HasFinished();
        }

        public void Start()
        {
            Sprite.Start();
        }

        public void Update(TimeSpan DeltaTime)
        {
            Sprite.ElapsedTime += DeltaTime;
        }
    }

    public class EntityStateMachine : StateMachine<EntStateKey, EntState> { }

    /// <summary>
    /// The basic entity. All actors and objects inherit from this
    /// </summary>
    [Data.DesignerCreatable]
    public class Entity
    {
        private static UInt64 nextId = 0; //generator for the unique IDs
        /// <summary>
        /// A unique ID for each entity
        /// Generated at runtime
        /// Primarily used for debugging
        /// </summary>
        [Data.NonSerialized]
        public UInt64 Id { get; } = (++nextId);

        /// <summary>
        /// The name of this entity. Typically used by other entities for locating (and therefore should be unique)
        /// </summary>
        [Data.NonDesigned]
        public string Name { get; set; } = null;

        /// <summary>
        /// The current position of the entity
        /// </summary>
        [Data.NonDesigned]
        public virtual Vector2 Position { get; set; }
        /// <summary>
        /// The (normalized) direction the entity is facing
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        [Data.NonDesigned]
        public virtual Vector2 Direction { get; set; } = Vector2.UnitX;
        /// <summary>
        /// The direction the entity is moving
        /// </summary>
        [Data.NonDesigned]
        public virtual Vector2 Velocity { get; set; } = Vector2.Zero;

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
        [Data.NonDesigned]
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
        /// Defines the entity's available and active state and handles transitions. Primarily for tracking actions
        /// </summary>
        /// <remarks>
        /// If State.Dead:
        ///     Entity will be removed if IsLooping is false (or if DestroyOffScreenIsDead applies)
        /// </remarks>
        public EntityStateMachine State { get; set; } = new EntityStateMachine();
 
        /// <summary>
        /// Destroy this entity if it goes off screen (becomes inactive) and is dead
        /// </summary>
        public bool DestroyIfDeadAndInactive { get; set; }

        /// <summary>
        /// Should the sprite always be drawn with the original sprite orientation?
        /// </summary>
        public bool AlwaysDrawUpright { get; set; } = false;

        /// <summary>
        /// Draw an outline around the sprite. If A is 0, ignored
        /// </summary>
        [Data.NonDesigned]
        public Color OutlineColor { get; set; } = Color.Transparent;

        /// <summary>
        /// When this entity was last spawned (in map time). Zero if destroyed or not spawned
        /// </summary>
        [Data.NonDesigned]
        public TimeSpan SpawnTime { get; set; } = TimeSpan.Zero; //todo: use map time

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
        /// The basic think function for this entity, called once a frame
        /// </summary>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void Think(System.TimeSpan DeltaTime)
        {
            State.Update(DeltaTime);
        }

        /// <summary>
        /// Called when the entity is spawned. Also called on deserialization
        /// </summary>
        public virtual void OnSpawn() { }
        /// <summary>
        /// Called when the entity is marked for deletion
        /// </summary>
        public virtual void OnDestroy() { }

        /// <summary>
        /// Called when there is a collision between this entity and another entity
        /// </summary>
        /// <param name="Collider">The entity collided with</param>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void OnEntityCollision(Entity Collider, Vector2 Point, TimeSpan DeltaTime) { }

        /// <summary>
        /// Called when there is a collision between this entity and the map
        /// </summary>
        /// <param name="Tile">The tile on the map where the collision occurred</param>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void OnMapCollision(Point Tile, Vector2 Point, TimeSpan DeltaTime) { }

        /// <summary>
        /// Called when there is a collision between this entity and a blob
        /// </summary>
        /// <param name="Type">The type of blob collided with</param>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void OnBlobCollision(BlobType Type, TimeSpan DeltaTime) { }
    }
}
