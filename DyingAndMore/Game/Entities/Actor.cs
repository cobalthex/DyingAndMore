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
    enum Factions : int
    {
        None = 0,
        Player = 1,
        Powerup = 2,
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

        public override void Think(GameTime Time)
        {
            Controller?.Think(Time);

            //todo: move to physics
            var vel = Velocity;

            if (vel == lastVelocity)
                vel = Vector2.Lerp(vel, Vector2.Zero, 10 * (float)Time.ElapsedGameTime.TotalSeconds);
            if (System.Math.Abs(vel.X) < 0.01f)
                vel.X = 0;
            if (System.Math.Abs(vel.Y) < 0.01f)
                vel.Y = 0;
            
            Velocity = vel;
            lastVelocity = Velocity;

            base.Think(Time);
        }

        private Controller cachedController = null;
        private System.TimeSpan bumpTime;
        public override void OnEntityCollision(Entity Collider, Vector2 Point, GameTime Time)
        {
            var actor = Collider as Actor;
            if (actor != null)
            {
                actor.cachedController = actor.controller;
                actor.controller = controller;
                controller = cachedController;
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
