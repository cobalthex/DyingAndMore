using System;
using Microsoft.Xna.Framework;
using Takai.Game;
using Takai;
using System.Collections.Generic;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Available factions. Work as bit flags (one actor can have multiple factions)
    /// </summary>
    [Flags]
    public enum Factions
    {
        None = 0,

        All = ~0,

        Player = (1 << 0), //only player(s) should have this faction

        Ally = (1 << 10), //the player or any actors allied with the player

        Enemy = (1 << 20), //enemy to player and allies

        Boss = (1 << 30),

    }

    public class ActorTriggerFilter : ITriggerFilter
    {
        /// <summary>
        /// A faction that this actor must be a part of to trigger
        /// </summary>
        public Factions factions;

        public bool CanTrigger(EntityInstance entity)
        {
            return (entity is ActorInstance actor) && actor.IsAlliedWith(factions);
        }
    }

    public class ActorClass : EntityClass
    {
        /// <summary>
        /// The default maximum allowed health of the entity (overhealing allowed)
        /// </summary>
        /// <remarks>Whenever this value is modified, the difference is added to current health</remarks>
        public int MaxHealth { get; set; }

        /// <summary>
        /// The field of vision of this actor. In Radians
        /// </summary>
        public float FieldOfView { get; set; } = MathHelper.PiOver4 * 3;

        /// <summary>
        /// How quickly this actor accelerates
        /// </summary>
        public Range<float> MoveForce { get; set; }

        //todo: make defaults a list, possibly controlled by difficulty/configuration

        //inherited
        public Range<float> MaxSpeed { get; set; }
        public Factions DefaultFactions { get; set; } = Factions.None;

        public Weapons.WeaponClass[] DefaultWeapon { get; set; } = null; //picks randomly
        public Controller[] DefaultController { get; set; } = null; //picks randomly

        /// <summary>
        /// The hud to display when controlling this actor
        /// </summary>
        public Takai.UI.Static Hud { get; set; }

        public override EntityInstance Instantiate()
        {
            return new ActorInstance(this);
        }
    }

    public class ActorInstance : EntityInstance
    {
        [Takai.Data.Serializer.ReadOnly]
        public new ActorClass Class
        {
            get => (ActorClass)base.Class;
            set
            {
                base.Class = value;
                if (base.Class != null)
                {
                    if (Class.Hud != null)
                    {
                        Hud = Class.Hud.CloneHierarchy();
                        Hud.BindTo(this);
                    }

                    //todo: re-evaluate if all of these should get set
                    MaxSpeed = Class.MaxSpeed.Random();
                    CurrentHealth = Class.MaxHealth;
                    Factions = Class.DefaultFactions;

                    Weapon = Util.Random(Class.DefaultWeapon)?.Instantiate();
                    Controller = Util.Random(Class.DefaultController)?.Clone();
                }
            }
        }

        /// <summary>
        /// The current faction. Typically used by the AI to determine enemies
        /// </summary>
        /// <remarks>0 is any/no faction</remarks>
        public Factions Factions { get; set; } = Factions.None;

        /// <summary>
        /// The current controller over this actor (null for none)
        /// </summary>
        /// <remarks>The controller's actor is automatically updated when this is set</remarks>
        public Controller Controller
        {
            get { return _controller; }
            set
            {
                if (_controller != null)
                    _controller.Actor = null;

                _controller = value;
                if (_controller != null)
                    _controller.Actor = this;
            }
        }
        private Controller _controller;

        /// <summary>
        /// The current health of the actor
        /// </summary>
        public float CurrentHealth { get; set; }

        /// <summary>
        /// All current conditions, and time remaining
        /// </summary>
        public Dictionary<ConditionClass, ConditionInstance> Conditions { get; set; }
            = new Dictionary<ConditionClass, ConditionInstance>();

        private Vector2 lastVelocity;

        public float MaxSpeed { get; set; }

        public Weapons.WeaponInstance Weapon
        {
            get => _weapon;
            set
            {
                if (_weapon == value)
                    return;

                if (_weapon != null)
                {
                    _weapon.Actor = null;
                    if (Hud != null && _weapon.Hud != null)
                        Hud.RemoveChild(_weapon.Hud);
                }

                _weapon = value;
                if (_weapon != null)
                {
                    _weapon.Actor = this;
                    if (Hud != null && _weapon.Hud != null)
                        Hud.AddChild(_weapon.Hud);
                }
            }
        }
        private Weapons.WeaponInstance _weapon;

        [Takai.Data.Serializer.Ignored] //todo: reload from map.squads
        public Squad Squad { get; internal set; }

        [Takai.Data.Serializer.Ignored]
        public Takai.UI.Static Hud { get; set; }

        public override Dictionary<string, CommandAction> Actions => new Dictionary<string, CommandAction>(base.Actions)
        {
            ["SetController"] = delegate (object controlObj)
            {
                if (controlObj is Controller controller)
                    Controller = controller;
            },
        };

        public ActorInstance() : this(null) { }
        public ActorInstance(ActorClass @class)
            : base(null)
        {
            Class = @class;
        }

        public override EntityInstance Clone()
        {
            var clone = (ActorInstance)base.Clone();

            clone._controller = null;
            clone.Controller = Controller?.Clone();

            clone._weapon = null;
            clone.Weapon = Weapon?.Clone();
            if (Hud != null)
            {
                clone.Hud = Hud.CloneHierarchy();
                clone.Hud.BindTo(clone);
            }
            return clone;
        }

        public override void Think(TimeSpan deltaTime)
        {
            foreach (var condition in Conditions)
                condition.Value.Update(this, deltaTime);
            //todo: remove inactive conditions?

            if (IsAlive)
                Controller?.Think(deltaTime);

            Weapon?.Think(deltaTime); //weapon can still fire if actor is dead

            //todo: move to physics
            var vel = Velocity;

            if (vel == lastVelocity)
                vel = Vector2.Lerp(vel, Vector2.Zero, 10 * (float)deltaTime.TotalSeconds);
            if (Math.Abs(vel.X) < 0.01f)
                vel.X = 0;
            if (Math.Abs(vel.Y) < 0.01f)
                vel.Y = 0;

            Velocity = vel;
            lastVelocity = Velocity;

            if (CurrentHealth <= 0 && Class.MaxHealth > 0)
                Kill();

            base.Think(deltaTime);
        }

        public override void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime)
        {
            Controller?.OnEntityCollision(collider, collision, deltaTime);
            //if (Collider is ActorInstance actor)
            //{
            //}
        }

        public override void OnMapCollision(Point tile, Vector2 point, TimeSpan deltaTime)
        {
            Controller?.OnMapCollision(tile, point, deltaTime);
        }

        public override void OnFluidCollision(FluidClass fluid, TimeSpan deltaTime)
        {
            Controller?.OnFluidCollision(fluid, deltaTime);
        }

        public virtual void TurnTowards(Vector2 direction)
        {
            Forward = direction; //todo: slerp?
        }

        public virtual void Accelerate(Vector2 direction)
        {
            var vel = Velocity + (direction * Class.MoveForce.Random());
            var lSq = vel.LengthSquared();
            if (lSq > MaxSpeed * MaxSpeed)
                vel = (vel / (float)Math.Sqrt(lSq)) * MaxSpeed;
            Velocity = vel;
        }

        /// <summary>
        /// Try to receive damage (instantly). Tests friendly fire, etc
        /// </summary>
        /// <param name="damage">the amount of damage to apply</param>
        /// <param name="source">The entity that is responsible for this damage</param>
        public void ReceiveDamage(float damage, EntityInstance source = null)
        {
            if (source != this && //can damage self
                (GameInstance.Current != null && !GameInstance.Current.Game.Configuration.AllowFriendlyFire) &&
                source is ActorInstance actor &&
                (actor.Factions & Factions) != 0)
                return;

            CurrentHealth -= damage;

            //todo: propertify (damage effect?)
            TintColor = Color.Tomato;
            TintColorDuration = TimeSpan.FromMilliseconds(50);
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\n" +
                   $"Health: {CurrentHealth}/{Class?.MaxHealth}\n" +
                   $"Weapon: {Weapon}\n" +
                   $"Controller: {Controller}";
        }

        #region Helpers

        /// <summary>
        /// Can this actor see a point given its field of view?
        /// (in world space)
        /// </summary>
        /// <param name="Point">The point to check</param>
        /// <returns>True if this entity is facing Point</returns>
        public bool IsFacing(Vector2 Point)
        {
            var diff = Point - WorldPosition;
            diff.Normalize();

            var dot = Vector2.Dot(WorldForward, diff);

            return (dot > (1 - (Class.FieldOfView / 2 / MathHelper.Pi)));
        }

        /// <summary>
        /// Is this entity behind another (The other entity cannot see this one)
        /// (in world space)
        /// </summary>
        /// <param name="Ent">The entity to check</param>
        /// <returns>True if this entity is behind Ent</returns>
        public bool IsBehind(ActorInstance Ent)
        {
            var diff = Ent.WorldPosition - WorldPosition;
            diff.Normalize();

            var dot = Vector2.Dot(diff, Ent.WorldForward);
            return (dot > (Class.FieldOfView / 2 / MathHelper.Pi) - 1);
        }

        public bool IsAlliedWith(Factions factions)
        {
            return (Factions & factions) != Factions.None;
        }

        #endregion
    }
}
