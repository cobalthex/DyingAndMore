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
        /// The mass of this object, in (virtual) kilograms 
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
        public TrailClass Trail { get; set; } // move to animations?

        public EntityClass()
        {
            EditorPreviewSprite = new Lazy<Graphics.Sprite>(delegate
            {
                if (Animations.TryGetValue("EditorPreview", out var state) ||
                    Animations.TryGetValue(DefaultBaseAnimation, out state))
                    return state.Sprite;
                return null;
            });
        }

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
        , Data.IReferenceable
        , ICollisionResolver
        , ISpawnable
    {
        /// <summary>
        /// A unique ID for each entity in the map
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// The class that this instance inherits from
        /// </summary>
        public virtual EntityClass Class
        {
            get => _Class;
            set
            {
                if (_Class == value)
                    return;

                _Class = value;
                PlayAnimation(Class.DefaultBaseAnimation);
                if (Class.Trail != null)
                    Trail = Class.Trail.Instantiate();
            }
        }
        private EntityClass _Class;

        /// <summary>
        /// A name for this instance, should be unique
        /// </summary>
        public string Name { get; set; } = null;

        public bool IsAlive { get; private set; } = true;

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
        public Dictionary<string, List<ICommand>> EventHandlers { get; set; }

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
        }

        /// <summary>
        /// Clone this entity
        /// Sets map to null and removes hierarchies
        /// </summary>
        /// <returns>Cloned entity</returns>
        public virtual EntityInstance Clone()
        {
            var clone = (EntityInstance)MemberwiseClone();
            clone.Id = 0;
            clone.Map = null;
            clone.WorldParent = null;
            clone._worldChildren = null; //clone children? (option?)
            clone.Position = clone.WorldPosition;
            clone.Forward = clone.WorldForward;
            if (clone.Name != null && !clone.Name.EndsWith(" (Clone)"))
                clone.Name += " (Clone)";
            return clone;
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
                Trail.Advance(WorldPosition, Vector2.Normalize(Velocity)); //todo: width?

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

            //trail should terminate where entity dies
            if (Trail != null)
                Trail.AddPoint(WorldPosition, Trail.CurrentDirection, true);

            IsAlive = false;
            PlayAnimation("Dead", () => { if (Class.DestroyOnDeath) Map?.Destroy(this); });
        }

        public virtual void Resurrect()
        {
            if (IsAlive)
                return;

            PlayAnimation(Class.DefaultBaseAnimation);
            IsAlive = true;
        }

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
                Trail.AddPoint(WorldPosition, Vector2.Normalize(Velocity));
        }
        /// <summary>
        /// Called when this instance is marked for deletion
        /// </summary>
        public virtual void OnDestroy(MapBaseInstance map)
        {
            if (Trail != null && Velocity != Vector2.Zero)
                Trail.AddPoint(WorldPosition, Vector2.Normalize(Velocity));

            if (!DisableNextDestructionEffect)
            {
                var fx = Class.DestructionEffect?.Instantiate(this);
                if (fx.HasValue)
                    Map.Spawn(fx.Value);
            }
            else
                DisableNextDestructionEffect = false;
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
            return $"Class: {(Class?.Name ?? "(none)")}\nName: {(Name ?? "(none)")}\nAlive: {IsAlive}";
        }

        public bool InRange(EntityInstance ent, float distanceBetween)
        {
            var d = Radius + ent.Radius + distanceBetween;
            return (Vector2.DistanceSquared(WorldPosition, ent.WorldPosition) <= (d * d));
        }
    }
}
