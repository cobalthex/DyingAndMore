using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static System.Math;

namespace Takai.Game
{
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
        public Dictionary<string, EntStateClass> States { get; set; }

        /// <summary>
        /// Destroy the sprite if it is dead and the animation is finished
        /// </summary>
        public bool DestroyOnDeath { get; set; } = true;

        /// <summary>
        /// Destroy this entity if it goes off screen (becomes inactive) and is dead
        /// </summary>
        public bool DestroyIfDeadAndInactive { get; set; } = false;

        public bool DestroyIfInactive { get; set; } = false;

        /// <summary>
        /// Should the sprite always be drawn with the original sprite orientation?
        /// </summary>
        public bool AlwaysDrawUpright { get; set; } = false;

        //idle audio (plays randomly when idle)

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

        [Data.Serializer.Ignored]
        public float Radius => State.Instance?.Class?.Radius ?? 1; //todo: aggregate all sprites + cache
        [Data.Serializer.Ignored]
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
                if (_class != null && State != null)
                    State.States = _class.States;
            }
        }
        private EntityClass _class;

        /// <summary>
        /// An optional entity to attach to. This entity's position becomes relative while attached
        /// </summary>
        public EntityInstance Parent
        {
            get => _parent;
            set
            {
                var nextPosition = Position;
                if (_parent != null)
                    nextPosition += _parent.Position;
                _parent = value;
                if (_parent != null)
                    nextPosition -= _parent.Position;
                Position = nextPosition;
            }
        }
        private EntityInstance _parent;

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
        public Vector2 Forward
        {
            get => _forward;
            set
            {
                _forward = value;
                if (Class != null && !Class.AlwaysDrawUpright)
                {
                    lastTransform.M11 = lastTransform.M22 = value.X;
                    lastTransform.M12 = -value.Y;
                    lastTransform.M21 = value.Y;
                    UpdateAxisAlignedBounds();
                }
            }
        }
        private Vector2 _forward = Vector2.UnitX;

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

        public StateMachine State
        {
            get => _state;
            set
            {
                if (_state != null)
                {
                    _state.StateComplete -= State_StateComplete;
                    _state.Transition -= State_Transition;
                }

                //todo: custom serialize state?
                _state = value;
                if (_state != null)
                {
                    _state.States = Class?.States;

                    _state.StateComplete += State_StateComplete;
                    _state.Transition += State_Transition;

                    lastTransform = GetTransform();
                    lastVisibleSize = GetVisibleSize();
                    UpdateAxisAlignedBounds();
                }
            }
        }
        private StateMachine _state = null;

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

            State = new StateMachine();
            State.TransitionTo(EntStateId.Idle, "Idle");
        }

        public Matrix GetTransform()
        {
            return new Matrix(
                Forward.X, -Forward.Y, Position.X, 0,
                Forward.Y, Forward.X, Position.Y, 0,
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
            return State?.Instance?.Class?.Sprite?.Size ?? new Point(1);
        }
        Point lastVisibleSize;

        /// <summary>
        /// Refresh the calculated bounds of this entity. usually called on spawn
        /// </summary>
        public void RefreshBounds()
        {
            Position = Position;
            Forward = Forward;
            lastVisibleSize = GetVisibleSize();
            UpdateAxisAlignedBounds();
        }

        /// <summary>
        /// Update the axis aligned bounds
        /// </summary>
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

        protected void State_Transition(object sender, TransitionEventArgs e)
        {
            lastVisibleSize = GetVisibleSize();
            UpdateAxisAlignedBounds();
        }

        protected void State_StateComplete(object sender, StateCompleteEventArgs e)
        {
            if (e.State == EntStateId.Dead && Class.DestroyOnDeath && Map != null)
                Map.Destroy(this);
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
    }
}

//todo: shorter instance/class names