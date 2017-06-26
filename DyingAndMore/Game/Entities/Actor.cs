using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;
using Takai.Graphics;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Available factions. Work as bit flags (one actor can have multiple factions)
    /// </summary>
    [System.Flags]
    enum Factions
    {
        None        = 0,
        Player      = (1 << 0),
        Enemy       = (1 << 1),
        Powerup     = (1 << 2),

        Virus       = (1 << 24),

        Common      = (1 << 56),
        //auto-immune
        //cancerous

        //max = 1 << 63
    }

    class Actor : Entity
    {
        private int maxHealth = 0;
        private Vector2 lastVelocity = Vector2.Zero;

        /// <summary>
        /// The current health of the actor
        /// </summary>
        [Takai.Data.NonDesigned]
        public int CurrentHealth { get; set; }

        /// <summary>
        /// The default maximum allowed health of the entity (overhealing allowed)
        /// </summary>
        /// <remarks>Whenever this value is modified, the difference is added to current health</remarks>
        public int MaxHealth
        {
            get { return maxHealth; }
            set
            {
                CurrentHealth += (value - maxHealth);
                maxHealth = value;
            }
        }

        public float FieldOfView { get; set; } = MathHelper.PiOver4 * 3;

        public float MaxSpeed { get; set; }
        public float MoveForce { get; set; }

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
                    controller.actor = null;

                controller = value;
                if (controller != null)
                    controller.actor = this;
            }
        }
        private Controller controller;

        public Weapons.Weapon PrimaryWeapon { get; set; } = null;
        public Weapons.Weapon AltWeapon { get; set; } = null;

        public Actor()
        {
        }

        public override object Clone()
        {
            var cloned = (Actor)base.Clone();

            if (cloned.controller != null)
                cloned.controller.actor = cloned;

            return cloned;
        }

        public override void Think(System.TimeSpan DeltaTime)
        {
            Controller?.Think(DeltaTime);

            //todo: move to physics
            var vel = Velocity;

            if (vel == lastVelocity)
                vel = Vector2.Lerp(vel, Vector2.Zero, 10 * (float)DeltaTime.TotalSeconds);
            if (System.Math.Abs(vel.X) < 0.01f)
                vel.X = 0;
            if (System.Math.Abs(vel.Y) < 0.01f)
                vel.Y = 0;

            Velocity = vel;
            lastVelocity = Velocity;

            if (CurrentHealth <= 0 && !(State.Is(EntStateKey.Dying) || State.Is(EntStateKey.Dead)))
                State.Transition(EntStateKey.Dying);

            if (State.Is(EntStateKey.Dying) && State.States[EntStateKey.Dying].HasFinished())
            {
                State.OverlaidStates.Clear();
                State.Transition(EntStateKey.Dead);
            }

            base.Think(DeltaTime);
        }

        public override void OnEntityCollision(Entity Collider, Vector2 Point, System.TimeSpan DeltaTime)
        {
            if (Collider is Actor actor)
            {
            }
        }

        #region Helpers

        /// <summary>
        /// Is this entity facing a point
        /// </summary>
        /// <param name="Point">The point to check</param>
        /// <returns>True if this entity is facing Point</returns>
        public bool IsFacing(Vector2 Point)
        {
            var diff = Point - Position;
            diff.Normalize();

            var dot = Vector2.Dot(Direction, diff);

            return (dot > (1 - (FieldOfView / MathHelper.Pi)));
        }

        /// <summary>
        /// Is this entity behind another (The other entity cannot see this one)
        /// </summary>
        /// <param name="Ent">The entity to check</param>
        /// <returns>True if this entity is behind Ent</returns>
        public bool IsBehind(Actor Ent)
        {
            var diff = Ent.Position - Position;
            diff.Normalize();

            var dot = Vector2.Dot(diff, Ent.Direction);
            return (dot > (Ent.FieldOfView / MathHelper.Pi) - 1);
        }

        public void Move(Vector2 Direction)
        {
            var vel = Velocity + (Direction * MoveForce);
            var lSq = vel.LengthSquared();
            if (lSq > MaxSpeed * MaxSpeed)
                vel = (vel / (float)System.Math.Sqrt(lSq)) * MaxSpeed;
            Velocity = vel;
        }

        #endregion
    }
}
