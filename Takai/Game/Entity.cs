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

    public class EntState : IState
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

        public bool IsOverlay { get; set; }

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

        public object Clone()
        {
            var clone = (EntState)MemberwiseClone();
            clone.Sprite = (Takai.Graphics.Sprite)Sprite.Clone();
            return clone;
        }
    }

    public class EntityStateMachine : StateMachine<EntStateKey, EntState> { }

    /// <summary>
    /// The basic entity. All actors and objects inherit from this
    /// </summary>
    [Data.DesignerCreatable]
    public class Entity : ICloneable
    {
        private static int nextId = 1; //generator for the unique (runtime) IDs

        /// <summary>
        /// A unique ID for each entity
        /// Generated at runtime
        /// Primarily used for debugging
        /// </summary>
        [Data.NonSerialized]
        public int Id { get; private set; } = (nextId++);

        /// <summary>
        /// The name of this entity. Typically used by other entities or scripts for referencing (and therefore should be unique)
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
        /// Determines if the entity is thinking
        /// </summary>
        public bool IsAlive { get; set; } = true;

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
        /// the axis aligned bounding box of this entity, based on radius
        /// </summary>
        [Data.NonSerialized]
        public Rectangle AxisAlignedBounds
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, (int)Radius, (int)Radius);
            }
        }

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
        /// Enumerate through active sprites for this entity
        /// </summary>
        public IEnumerable<Graphics.Sprite> Sprites
        {
            get
            {
                if (State.States.TryGetValue(State.BaseState, out var state))
                    yield return state.Sprite;

                foreach (var overlay in State.OverlaidStates)
                {
                    if (State.States.TryGetValue(overlay, out state))
                        yield return state.Sprite;
                }
            }
        }

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
        public TimeSpan SpawnTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The group an entity belongs to
        /// </summary>
        [Data.NonSerialized]
        public Group Group
        {
            get { return group; }
            set
            {
                if (group != null)
                    group.Entities.Remove(this);

                group = value;

                if (group != null)
                    group.Entities.Add(this);
            }
        }
        private Group group;

        public Entity() { }

        /// <summary>
        /// Clone this entity
        /// </summary>
        /// <returns>A cloned entity</returns>
        /// <remarks>Map information is not cloned</remarks>
        public virtual object Clone()
        {
            var cloned = (Entity)MemberwiseClone();

            cloned.Id = (nextId++);
            cloned.Map = null;
            cloned.State = (EntityStateMachine)State.Clone();

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
        /// Called when there is a collision between this entity and a Fluid
        /// </summary>
        /// <param name="Type">The type of Fluid collided with</param>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void OnFluidCollision(FluidType Type, TimeSpan DeltaTime) { }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
