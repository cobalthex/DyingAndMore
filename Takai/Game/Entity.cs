﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static System.Math;

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

        //game specific (come up with better way?)

        ChargeWeapon = 128,
        DischargeWeapon
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
            get => Sprite?.TotalLength ?? TimeSpan.Zero;
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
            return ElapsedTime >= Class.TotalTime;
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
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                lastTransform.Translation = new Vector3(value, 0);
                UpdateAxisAlignedBounds();
            }
        }
        private Vector2 _position;

        /// <summary>
        /// The (normalized) direction the entity is facing
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                if (Class != null && !Class.AlwaysDrawUpright)
                {
                    lastTransform.M11 = lastTransform.M22 = value.X;
                    lastTransform.M12 = -value.Y;
                    lastTransform.M21 = value.Y;
                    UpdateAxisAlignedBounds();
                }
            }
        }
        private Vector2 _direction = Vector2.UnitX;

        /// <summary>
        /// The velocity of the entity, separate from the direction
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        /// <summary>
        /// the axis aligned bounding box of this entity
        /// updated whenever the state, position, or direction changes
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle AxisAlignedBounds { get; private set; }

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

        public EntityInstance() : this(null) { }
        public EntityInstance(EntityClass @class)
        {
            Class = @class;
            State.TransitionTo(EntStateId.Idle);

            State.StateComplete += State_StateComplete;
            State.Transition += State_Transition;

            lastTransform = GetTransform();
            lastVisibleSize = GetVisibleSize();
            UpdateAxisAlignedBounds();
        }

        public Matrix GetTransform()
        {
            return new Matrix(
                Direction.X, -Direction.Y, Position.X, 0,
                Direction.Y, Direction.X, Position.Y, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
        }
        Matrix lastTransform;

        /// <summary>
        /// Get the size of the visible sprites
        /// </summary>
        /// <returns>The calculated extent</returns>
        public Point GetVisibleSize()
        {
            var p = new Point();
            foreach (var state in ActiveStates)
            {
                if (state.Class?.Sprite != null)
                {
                    p.X = MathHelper.Max(p.X, state.Class.Sprite.Width);
                    p.Y = MathHelper.Max(p.Y, state.Class.Sprite.Height);
                }
            }
            return p;
        }
        Point lastVisibleSize;

        protected void UpdateAxisAlignedBounds()
        {
            var r = new Rectangle(
                lastVisibleSize.X / -2,
                lastVisibleSize.Y / -2,
                lastVisibleSize.X,
                lastVisibleSize.Y
            );

            //todo: handle origin

            var transform = lastTransform;

            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);

            var v = Vector2.Transform(new Vector2(r.X, r.Y), transform);
            min = Vector2.Min(min, v); max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(r.X + r.Width, r.Y), transform);
            min = Vector2.Min(min, v); max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(r.X + r.Width, r.Y + r.Height), transform);
            min = Vector2.Min(min, v); max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(r.X, r.Y + r.Height), transform);
            min = Vector2.Min(min, v); max = Vector2.Max(max, v);

            AxisAlignedBounds = new Rectangle(min.ToPoint(), (max - min).ToPoint());
        }

        protected void State_Transition(object sender, TransitionEventArgs<EntStateInstance> e)
        {
            lastVisibleSize = GetVisibleSize();
            UpdateAxisAlignedBounds();
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