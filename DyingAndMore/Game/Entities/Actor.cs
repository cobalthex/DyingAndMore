using System;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Available factions. Work as bit flags (one actor can have multiple factions)
    /// </summary>
    [Flags]
    public enum Factions
    {
        None        = 0,
        Player      = (1 << 0),
        Enemy       = (1 << 1),
        Boss        = (1 << 2),

        Powerup     = (1 << 4),

        Virus       = (1 << 24),

        Common      = (1 << 56),
        //auto-immune
        //cancerous

        //max = 1 << 63
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
        public float MoveForce { get; set; }

        //inherited
        public Range<float> MaxSpeed { get; set; }
        public Weapons.WeaponClass DefaultWeapon { get; set; } = null;
        public Factions DefaultFaction { get; set; } = Factions.None;
        public Controller DefaultController { get; set; } = null;

        public override EntityInstance Create()
        {
            return new ActorInstance(this);
        }
    }

    public class ActorInstance : EntityInstance
    {
        public override EntityClass Class
        {
            get => base.Class;
            set
            {
                System.Diagnostics.Contracts.Contract.Assert(value == null || value is ActorClass);
                base.Class = value;
                _class = value as ActorClass;
            }
        }
        private ActorClass _class;

        /// <summary>
        /// The current faction. Typically used by the AI to determine enemies
        /// </summary>
        /// <remarks>0 is any/no faction</remarks>
        public Factions Faction { get; set; } = Factions.None;

        /// <summary>
        /// The current controller over this actor (null for none)
        /// </summary>
        /// <remarks>The controller's actor is automatically updated when this is set</remarks>
        public Controller Controller
        {
            get { return controller; }
            set
            {
                if (controller != null)
                    controller.Actor = null;

                controller = value;
                if (controller != null)
                    controller.Actor = this;
            }
        }
        private Controller controller;

        /// <summary>
        /// The current health of the actor
        /// </summary>
        public int CurrentHealth { get; set; }

        private Vector2 lastVelocity;

        #region Inherited

        public float MaxSpeed { get; set; }

        public Weapons.WeaponInstance Weapon
        {
            get => _weapon;
            set
            {
                if (_weapon != value)
                {
                    if (_weapon != null)
                        _weapon.Actor = null;

                    _weapon = value;
                    _weapon.Actor = this;
                }
            }
        }
        private Weapons.WeaponInstance _weapon;

        #endregion

        public ActorInstance() : this(null) { }
        public ActorInstance(ActorClass @class)
            : base(@class)
        {
            if (Class != null)
            {
                MaxSpeed      = RandomRange.Next(_class.MaxSpeed);
                CurrentHealth = _class.MaxHealth;
                Weapon        = _class.DefaultWeapon?.Create();
                Faction       = _class.DefaultFaction;

                if (_class.DefaultController != null)
                {
                    Controller = (Controller)_class.DefaultController.Clone();
                    Controller.Actor = this;
                }
            }
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (IsAlive)
            {
                Controller?.Think(deltaTime);
                Weapon?.Think(deltaTime);
            }

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

            if (CurrentHealth <= 0)
                IsAlive = false;

            base.Think(deltaTime);
        }

        public override void OnEntityCollision(EntityInstance Collider, Vector2 Point, TimeSpan DeltaTime)
        {
            if (Collider is ActorInstance actor)
            {
            }
        }

        public void Accelerate(Vector2 direction)
        {
            var vel = Velocity + (direction * _class.MoveForce);
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
        public void ReceiveDamage(int damage, EntityInstance source = null)
        {
            if ((GameInstance.Current.configuration == null ||
                !GameInstance.Current.configuration.AllowFriendlyFire) &&
                source is ActorInstance actor &&
                (actor.Faction & Faction) != 0)
                return;

            CurrentHealth -= damage;
        }

        #region Helpers

        /// <summary>
        /// Can this actor see a point given its field of view?
        /// </summary>
        /// <param name="Point">The point to check</param>
        /// <returns>True if this entity is facing Point</returns>
        public bool IsFacing(Vector2 Point)
        {
            var diff = Point - Position;
            diff.Normalize();

            var dot = Vector2.Dot(Forward, diff);

            return (dot > (1 - (_class.FieldOfView / 2 / MathHelper.Pi)));
        }

        /// <summary>
        /// Is this entity behind another (The other entity cannot see this one)
        /// </summary>
        /// <param name="Ent">The entity to check</param>
        /// <returns>True if this entity is behind Ent</returns>
        public bool IsBehind(ActorInstance Ent)
        {
            var diff = Ent.Position - Position;
            diff.Normalize();

            var dot = Vector2.Dot(diff, Ent.Forward);
            return (dot > (_class.FieldOfView / 2 / MathHelper.Pi) - 1);
        }

        public bool IsAlliedWith(Factions factions)
        {
            return (Faction & factions) != Factions.None;
        }

        #endregion
    }
}
