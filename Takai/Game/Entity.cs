using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// All of the possible entity states
    /// </summary>
    public enum EntState
    {
        Idle = 0,
        Dead,
        Down,
        Inactive,
        Active,
    }

    /// <summary>
    /// The basic entity. All actors and objects inherit from this
    /// </summary>
    [Data.DesignerCreatable]
    public class Entity
    {
        /// <summary>
        /// The name of this entity. Typically used by other entities for locating
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
        /// Different animation states for the entity
        /// </summary>
        public Dictionary<EntState, Takai.Graphics.Graphic> States { get; set; } = null;

        /// <summary>
        /// Get or set the current state.
        /// Automatically updates entity sprite on set
        /// </summary>
        /// <remarks>Does nothing if the state does not exist</remarks>
        [Takai.Data.NonDesigned]
        public EntState CurrentState
        {
            get
            {
                return currentState;
            }
            set
            {
                currentState = value;
                if (States != null && States.ContainsKey(value))
                {
                    Sprite = States[currentState];
                    Sprite.Restart();
                }

                if (DestroyOnDeath && currentState == EntState.Dead)
                    Map?.Destroy(this);
            }
        }
        private EntState currentState = EntState.Idle;

        /// <summary>
        /// Remove this entity from the map when it dies (CurrentState == Dead)
        /// </summary>
        /// <remarks>Will wait for animation to finish (if not looping)</remarks>
        public bool DestroyOnDeath { get; set; } = true;

        /// <summary>
        /// Destroy this entity if it goes off screen and is dead
        /// </summary>
        public bool DestroyOffScreenIfDead { get; set; }

        /// <summary>
        /// The active sprite for this entity. Usually updated by the state machine
        /// Can be updated by components
        /// </summary>
        public Graphics.Graphic Sprite { get; set; } = null;

        /// <summary>
        /// Should the sprite always display upright (angle of sprite does not affect display)?
        /// </summary>
        public bool AlwaysUpright { get; set; } = false;

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
        public virtual void Think(System.TimeSpan DeltaTime) { }

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
