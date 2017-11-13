using System;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// Describes a single type of entity. Actors, etc. inherit from this
    /// </summary>
    public abstract partial class EntityClass : IObjectClass<EntityInstance>
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
        /// The mass of this object.
        /// 0 for light objects
        /// 1 for 'normal' mass objects
        /// 10 for very heavy objects
        /// </summary>
        public float Mass { get; set; } = 1;

        /// <summary>
        /// The drag coefficient of this entity
        /// </summary>
        public float Drag { get; set; } = 0;

        /// <summary>
        /// Trace (raycast) queries should ignore this entity
        /// </summary>
        public bool IgnoreTrace { get; set; } = false;

        /// <summary>
        /// Destroy the sprite if it is dead and the animation is finished
        /// </summary>
        public bool DestroyOnDeath { get; set; } = true;

        /// <summary>
        /// Destroy this entity if it goes off screen (becomes inactive) and is dead
        /// </summary>
        public bool DestroyIfDeadAndOffscreen { get; set; } = false;

        public bool DestroyIfOffscreen { get; set; } = false;

        /// <summary>
        /// Should the sprite always be drawn with the original sprite orientation?
        /// </summary>
        public bool AlwaysDrawUpright { get; set; } = false;

        /// <summary>
        /// An effect created at the entity's position when its spawned in a map
        /// </summary>
        public EffectsClass SpawnEffect { get; set; }
        /// <summary>
        /// An effect created at the entity's position when it is destroyed from a map
        /// </summary>
        public EffectsClass DestructionEffect { get; set; }

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
    public abstract partial class EntityInstance : IObjectInstance<EntityClass>
    {
        private static int nextId = 1; //generator for the unique (runtime) IDs

        /// <summary>
        /// A unique ID for each entity
        /// Generated at runtime
        /// Primarily used for debugging
        /// </summary>
        [Data.Serializer.Ignored]
        public int Id { get; private set; } = (nextId++); //todo: serializable ID

        [Data.Serializer.Ignored]
        public float Radius { get; private set; }
        [Data.Serializer.Ignored]
        public float RadiusSq => Radius * Radius; //todo: cache

        /// <summary>
        /// The class that this instance inherits from
        /// </summary>
        public virtual EntityClass Class { get; set; }

        /// <summary>
        /// A name for this instance, should be unique
        /// </summary>
        public string Name { get; set; } = null;

        public bool IsAlive
        {
            get => isAlive;
            set
            {
                //play death animation
                if (value == false && isAlive)
                    PlayAnimation("Dead", () => { if (Class.DestroyOnDeath) Map?.Destroy(this); });
                isAlive = value;
            }
        }
        private bool isAlive = true;

        //todo: parent/child relationships

        /// <summary>
        /// The current position of the entity
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                var diff = value - _position;
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

        /// <summary>
        /// Draw an outline around the sprite. If A is 0, ignored
        /// </summary>
        public Color OutlineColor { get; set; } = Color.Transparent;

        /// <summary>
        /// The map the entity is in
        /// </summary>
        [Data.Serializer.Ignored]
        public MapInstance Map { get; internal set; } = null;

        /// <summary>
        /// When this entity was last spawned (in map time). Zero if destroyed or not spawned
        /// </summary>
        public TimeSpan SpawnTime { get; set; } = TimeSpan.Zero;

        public EntityInstance() : this(null) { }
        public EntityInstance(EntityClass @class)
        {
            Class = @class;

            //todo: default animation
            PlayAnimation("Idle");
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
        Point lastVisibleSize;

        /// <summary>
        /// Refresh the calculated bounds of this entity. usually called on spawn
        /// </summary>
        public void RefreshBounds()
        {
            Position = Position;
            Forward = Forward;
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

            r = new Rectangle(min.ToPoint(), (max - min).ToPoint());


            //todo: parent/child relationships

            AxisAlignedBounds = r;
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
            UpdateAnimations(deltaTime);
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

        public float ForwardSpeed()
        {
            return Vector2.Dot(Forward, Velocity);
        }
    }
}