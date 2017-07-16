using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// All of the possible entity states
    /// </summary>
    public enum EntStateId
    {
        Invalid,
        Dead,
        Idle,
        Inactive,
        Active,
    }

    public class EntStateClass : IStateClass<EntStateId, EntStateInstance>
    {
        public string Name { get; set; }

        [Data.Serializer.Ignored]
        public string File { get; set; } = null;

        public bool IsOverlay { get; set; } = false;

        public Graphics.Sprite Sprite { get; set; }

        //Sound

        //todo: cache
        public float Radius =>
            Sprite != null ? MathHelper.Max(Sprite.Width, Sprite.Height) / 2 : 1;

        [Data.Serializer.Ignored]
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

        [Data.Serializer.Ignored]
        public TimeSpan TotalTime
        {
            get => Sprite.TotalLength;
            set { } //todo
        }

        public EntStateInstance Create()
        {
            return new EntStateInstance()
            {
                Class = this,
                ElapsedTime = TimeSpan.Zero
            };
        }
    }

    public class EntStateInstance : IStateInstance<EntStateId, EntStateClass>
    {
        public EntStateId Id { get; set; }

        [Data.Serializer.Ignored]
        public EntStateClass Class { get; set; }

        /// <summary>
        /// The current elapsed time of this state
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        public bool HasFinished()
        {
            return ElapsedTime > Class.TotalTime;
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public void Update(TimeSpan deltaTime)
        {

        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }

    /// <summary>
    /// Describes a single type of entity. Actors, etc. inherit from this
    /// </summary>
    [Data.DesignerModdable]
    public abstract class EntityClass : IObjectClass<EntityInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// The name of this entity. Typically used by other entities or scripts for referencing (and therefore should be unique)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Determines if this entity is always part of the active set and therefore always updated
        /// </summary>
        /// <remarks>This is typically used for things like projectiles</remarks>
        public bool AlwaysActive { get; set; } = false;

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
        /// All of this entity's available states
        public Dictionary<EntStateId, EntStateClass> States { get; set; }

        /// <summary>
        /// Destroy the sprite if it is dead and the animation is finished
        /// </summary>
        public bool DestroyOnDeath { get; set; } = true;

        /// <summary>
        /// Destroy this entity if it goes off screen (becomes inactive) and is dead
        /// </summary>
        public bool DestroyIfDeadAndInactive { get; set; } = false;

        /// <summary>
        /// Should the sprite always be drawn with the original sprite orientation?
        /// </summary>
        public bool AlwaysDrawUpright { get; set; } = false;

        public EntityClass() { }

        public abstract EntityInstance Create();

        public override string ToString()
        {
            return (Name ?? base.ToString());
        }
    }

    /// <summary>
    /// A single instance of an entity in a map. Mostly logic handled through <see cref="EntityClass"/>
    /// </summary>
    [Takai.Data.DerivedTypeDeserialize(typeof(EntityInstance), "DerivedDeserialize")]
    public abstract class EntityInstance : IObjectInstance<EntityClass>
    {
        private static int nextId = 1; //generator for the unique (runtime) IDs

        /// <summary>
        /// A unique ID for each entity
        /// Generated at runtime
        /// Primarily used for debugging
        /// </summary>
        [Data.Serializer.Ignored]
        public int Id { get; private set; } = (nextId++); //todo: map-specific id

        public float Radius => State.BaseState?.Class?.Radius ?? 1; //todo: aggregate all sprites + cache
        public float RadiusSq => Radius * Radius; //todo: cache

        /// <summary>
        /// The class that this instance inherits from
        /// </summary>
        public virtual EntityClass Class
        {
            get => _class;
            set
            {
                _class = value;
                State.States = _class.States;
            }
        }
        private EntityClass _class;

        /// <summary>
        /// A name for this instance, should be unique
        /// </summary>
        [Data.DesignerModdable]
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
        /// The velocity of the entity, separate from the direction
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Velocity { get; set; } = Vector2.UnitX;

        /// <summary>
        /// the axis aligned bounding box of this entity, based on radius
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle AxisAlignedBounds
        {
            get
            {
                return new Rectangle(
                    (int)Position.X - (int)Radius,
                    (int)Position.Y - (int)Radius,
                    (int)Radius * 2,
                    (int)Radius * 2
                );
            }
        }

        [Data.CustomSerialize("CustomSerializeState")]
        public StateMachine<EntStateId, EntStateClass, EntStateInstance> State { get; set; }
            = new StateMachine<EntStateId, EntStateClass, EntStateInstance>();

        private object CustomSerializeState()
        {
            return new Dictionary<string, object>
            {
                { "BaseState", State.BaseState },
                { "OverlaidStates", State.OverlaidStates },
                { "Transitions", State.Transitions }
            };
        }

        /// <summary>
        /// Enumerate through the classes of active states
        /// </summary>
        [Data.Serializer.Ignored]
        public IEnumerable<EntStateInstance> ActiveStates
        {
            get
            {
                yield return State.BaseState;
                foreach (var state in State.OverlaidStates)
                    yield return state.Value;
            }
        }

        /// <summary>
        /// Draw an outline around the sprite. If A is 0, ignored
        /// </summary>
        public Color OutlineColor { get; set; } = Color.Transparent;

        /// <summary>
        /// The map the entity is in
        /// </summary>
        [Data.Serializer.Ignored]
        public Map Map { get; internal set; } = null;

        /// <summary>
        /// When this entity was last spawned (in map time). Zero if destroyed or not spawned
        /// </summary>
        public TimeSpan SpawnTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The group an entity belongs to
        /// </summary>
        [Data.Serializer.Ignored]
        public Group Group
        {
            get { return _group; }
            set
            {
                _group = value;
                //if (group != null)
                //    group.Entities.Remove(this);

                //group = value;

                //if (group != null)
                //    group.Entities.Add(this);

                throw new NotImplementedException();
            }
        }
        private Group _group;

        public EntityInstance() : this(null) { }
        public EntityInstance(EntityClass @class)
        {
            Class = @class;
            State.TransitionTo(EntStateId.Idle);

            State.StateComplete += State_StateComplete;
            State.Transition += State_Transition;
        }

        protected void State_Transition(object sender, TransitionEventArgs<EntStateInstance> e)
        {

        }

        protected void State_StateComplete(object sender, StateCompleteEventArgs<EntStateInstance> e)
        {
            if (e.State.Id == EntStateId.Dead && Class.DestroyOnDeath && Map != null)
                Map.Destroy(this);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityInstance ent && ent.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return (Class?.Name ?? base.ToString()) + $"({Id})";
        }

        /// <summary>
        /// The basic think function for this entity, called once a frame
        /// </summary>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void Think(TimeSpan deltaTime)
        {
            State.Update(deltaTime);
        }

        /// <summary>
        /// Called when this instance is spawned. Also called on deserialization
        /// </summary>
        public virtual void OnSpawn() { }
        /// <summary>
        /// Called when this instance is marked for deletion
        /// </summary>
        public virtual void OnDestroy() { }

        /// <summary>
        /// Called when there is a collision between this instance and another
        /// </summary>
        /// <param name="Collider">The instance collided with</param>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void OnEntityCollision(EntityInstance Collider, Vector2 Point, TimeSpan DeltaTime) { }

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
        public virtual void OnFluidCollision(FluidClass Type, TimeSpan DeltaTime) { }

        protected void DerivedDeserializer(Dictionary<string, object> props)
        {
            if (State.BaseState != null && State.States.TryGetValue(State.BaseState.Id, out var k))
                State.BaseState.Class = k;

            foreach (var state in State.OverlaidStates)
            {
                if (state.Value != null && State.States.TryGetValue(state.Key, out k))
                    state.Value.Class = k;
            }
        }
    }
}

//todo: shorter instance/class names