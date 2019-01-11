using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public interface ISpawnable
    {
        void OnSpawn(MapBaseInstance map);
        void OnDestroy(MapBaseInstance map);
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class EntityAsReferenceAttribute : Attribute { }

    /// <summary>
    /// Describes a single type of entity. Actors, etc. inherit from this
    /// </summary>
    public partial class EntityClass : Data.INamedClass<EntityInstance>
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
        public bool AlwaysDrawUpright { get; set; } = false; //todo: move to animation

        /// <summary>
        /// An effect created at the entity's position when its spawned in a map
        /// </summary>
        public EffectsClass SpawnEffect { get; set; }
        /// <summary>
        /// An effect created at the entity's position when it is destroyed from a map
        /// </summary>
        public EffectsClass DestructionEffect { get; set; }

        /// <summary>
        /// A trail that is applied on spawn
        /// </summary>
        public TrailClass Trail { get; set; }

        /// <summary>
        /// Code defined events that this entity can trigger
        /// </summary>
        public virtual HashSet<string> Events { get; }

        public EntityClass() { }

        public virtual EntityInstance Instantiate()
        {
            return new EntityInstance(this);
        }

        public override string ToString()
        {
            return $"{GetType().Name} ({Name})";
        }
    }

    public interface ICollisionResolver
    {
        /// <summary>
        /// Called when there is a collision between this instance and another
        /// </summary>
        /// <param name="collider">The instance collided with</param>
        /// <param name="deltaTime">How long since the last frame (in map time)</param>
        void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime);

        /// <summary>
        /// Called when there is a collision between this entity and the map
        /// </summary>
        /// <param name="tile">The tile on the map where the collision occurred</param>
        /// <param name="deltaTime">How long since the last frame (in map time)</param>
        void OnMapCollision(Point tile, Vector2 point, TimeSpan deltaTime);

        /// <summary>
        /// Called when there is a collision between this entity and a Fluid
        /// </summary>
        /// <param name="fluid">The type of Fluid collided with</param>
        /// <param name="deltaTime">How long since the last frame (in map time)</param>
        void OnFluidCollision(FluidClass fluid, TimeSpan deltaTime);
    }

    /// <summary>
    /// A single instance of an entity in a map.
    /// </summary>
    public partial class EntityInstance 
        : Data.IInstance<EntityClass>
        , Data.Serializer.IReferenceable
        , ICollisionResolver
        , ISpawnable
    {
        /// <summary>
        /// A unique ID for each entity in the map
        /// </summary>
        public int Id { get; set; } = 0;

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

        public bool IsAlive { get; private set; } = true;

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
                AxisAlignedBounds = new Rectangle(
                    AxisAlignedBounds.X + (int)diff.X,
                    AxisAlignedBounds.Y + (int)diff.Y,
                    AxisAlignedBounds.Width,
                    AxisAlignedBounds.Height
                );
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
        /// The velocity of the entity, separate from <see cref="Forward"/>
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        /// <summary>b
        /// the axis aligned bounding box of this entity
        /// updated whenever the state, position, or direction changes
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle AxisAlignedBounds { get; private set; }

        internal Rectangle lastAABB; //used for tracking movement in spacial grid

        /// <summary>
        /// Draw an outline around the sprite. If A is 0, ignored
        /// </summary>
        public Color OutlineColor { get; set; }
        /// <summary>
        /// A color to tint the entity by. e.g. as a damage indicator
        /// </summary>
        public Color TintColor { get; set; } = Color.White;
        public TimeSpan TintColorDuration { get; set; }

        public TrailInstance Trail { get; set; }

        /// <summary>
        /// The map the entity is in
        /// </summary>
        [Data.Serializer.Ignored]
        public MapBaseInstance Map { get; internal set; } = null;

        /// <summary>
        /// When this entity was last spawned (in map time). Zero if destroyed or not spawned
        /// </summary>
        public TimeSpan SpawnTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Disable the next destruction effect.
        /// Useful for when playing custom effects/animations on destruction
        /// </summary>
        [Data.Serializer.Ignored]
        public bool DisableNextDestructionEffect { get; set; } = false;

        /// <summary>
        /// Event handlers for this entity. Set by <see cref="EntityClass.Events"/>
        /// </summary>
        public Dictionary<string, List<GameCommand>> EventHandlers { get; set; }

        /// <summary>
        /// Actions that this entity can perform
        /// </summary>
        public virtual Dictionary<string, CommandAction> Actions => new Dictionary<string, CommandAction>
        {
            //todo: this needs a better format

            ["Kill"] = (ignored) => Kill(),
            ["Resurrect"] = (ignored) => Resurrect(),
            ["ApplyEffect"] = delegate (object effect)
            {
                if (effect is EffectsClass ec && Map != null) //todo: pass effects instance?
                {
                    var ei = ec.Instantiate(null, this);
                    ei.Position = Position;
                    Map.Spawn(ei);
                }
            },
            ["PlayAnimation"] = delegate (object animation)
            {
                if (animation is string animName)
                    PlayAnimation(animName);
                else if (animation is AnimationClass animClass)
                    PlayAnimation(animClass);
            },
            ["StopAnimation"] = delegate (object animation)
            {
                if (animation is string animName)
                    StopAnimation(animName);
                else if (animation is AnimationClass animClass)
                    StopAnimation(animClass);
            },
            //set trail, tint, outline?
        }; //todo: create this in a better way

        public EntityInstance() : this(null) { }
        public EntityInstance(EntityClass @class)
        {
            Class = @class;
            if (Class == null)
                return;

            PlayAnimation(Class.DefaultBaseAnimation);
            if (Class.Trail != null)
                Trail = Class.Trail.Instantiate();
        }

        public virtual EntityInstance Clone()
        {
            var clone = (EntityInstance)MemberwiseClone();
            clone.Id = 0;
            clone.Map = null;
            if (clone.Name != null && !clone.Name.EndsWith(" (Clone)"))
                clone.Name += " (Clone)";
            return clone;
        }

        Matrix lastTransform = Matrix.Identity;
        Point lastVisibleSize = new Point(1, 1);

        /// <summary>
        /// Update the axis aligned bounds
        /// </summary>
        internal void UpdateAxisAlignedBounds()
        {
            var r = new Rectangle(
                lastVisibleSize.X / -2,
                lastVisibleSize.Y / -2,
                lastVisibleSize.X,
                lastVisibleSize.Y
            );

            //todo: handle origin

            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);

            var v = Vector2.Transform(new Vector2(r.X, r.Y), lastTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(r.X + r.Width, r.Y), lastTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(r.X + r.Width, r.Y + r.Height), lastTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            v = Vector2.Transform(new Vector2(r.X, r.Y + r.Height), lastTransform);
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);

            var size = max - min;
            r = new Rectangle((int)min.X, (int)min.Y, (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));

            //todo: parent/child relationships

            AxisAlignedBounds = r;
        }

        public override string ToString()
        {
            return $"{GetType().Name} ({Class?.Name}) {{{Id}}}";
        }

        /// <summary>
        /// The basic think function for this entity, called once a frame
        /// </summary>
        /// <param name="DeltaTime">How long since the last frame (in map time)</param>
        public virtual void Think(TimeSpan deltaTime)
        {
            if (Trail != null && Velocity != Vector2.Zero)
                Trail.Advance(Position, Vector2.Normalize(Velocity)); //todo: width?

            if (TintColorDuration > TimeSpan.Zero)
            {
                TintColorDuration -= deltaTime;
                if (TintColorDuration <= TimeSpan.Zero)
                    TintColor = Color.White;
            }
        }

        public virtual void Kill()
        {
            if (!IsAlive)
                return;

            if (Trail != null)
                Trail.AddPoint(Position, Trail.CurrentDirection, true);

            PlayAnimation("Dead", () => { if (Class.DestroyOnDeath) Map?.Destroy(this); });
            IsAlive = false;
        }

        public virtual void Resurrect()
        {
            if (IsAlive)
                return;

            PlayAnimation(Class.DefaultBaseAnimation);
            IsAlive = true;
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

        /// <summary>
        /// Called when this instance is spawned
        /// </summary>
        public virtual void OnSpawn(MapBaseInstance map)
        {
            if (baseAnimation.Class == null)
                PlayAnimation(Class.DefaultBaseAnimation);

            var fx = Class.SpawnEffect?.Instantiate(this);
            if (fx.HasValue)
                Map.Spawn(fx.Value);

            if (Trail != null && Velocity != Vector2.Zero)
                Trail.AddPoint(Position, Vector2.Normalize(Velocity));
        }
        /// <summary>
        /// Called when this instance is marked for deletion
        /// </summary>
        public virtual void OnDestroy(MapBaseInstance map)
        {
            if (Trail != null && Velocity != Vector2.Zero)
                Trail.AddPoint(Position, Vector2.Normalize(Velocity));

            if (!DisableNextDestructionEffect)
            {
                var fx = Class.DestructionEffect?.Instantiate(this);
                if (fx.HasValue)
                    Map.Spawn(fx.Value);
            }
            else
                DisableNextDestructionEffect = false;
        }

        public float ForwardSpeed()
        {
            return Vector2.Dot(Forward, Velocity);
        }

        /// <summary>
        /// Is this entity alive and owned by the specified map
        /// </summary>
        /// <param name="map">The map to check</param>
        /// <returns>True if this entity is alive and in this map</returns>
        public bool IsAliveIn(MapBaseInstance map)
        {
            return IsAlive && Map == map;
        }

        /// <summary>
        /// Trigger an event
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <returns>True if there are event handlers for the event</returns>
        protected virtual bool TriggerEvent(string eventName)
        {
            if (EventHandlers == null || !EventHandlers.TryGetValue(eventName, out var commands))
                return false;

            foreach (var command in commands)
                command.Invoke(Map);

            return true;
        }

        public virtual string GetDebugInfo()
        {
            return $"Name: {(Name ?? "(none)")}\nAlive: {IsAlive}";
        }
    }
}

/*
 * Entity commands and actions
 *  Entities have events that are defined by the entity (e.g OnDeath)
 *  Call any EventHandlers
 *  EventHandlers have Commands. Commands can trigger actions, either on entities, game settings, etc
*/