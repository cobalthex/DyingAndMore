using Microsoft.Xna.Framework;
using Takai.Graphics;

namespace DyingAndMore.Entities
{
    class Actor : Takai.Game.Entity
    {
        public float FieldOfView { get; set; } = MathHelper.PiOver4 * 3;

        public float MaxSpeed { get; set; } = 400;
        public float MoveForce { get; set; } = 600;
        
        protected Components.Health health;
        protected Components.AnimState state;

        public Actor()
        {
            health = AddComponent<Components.Health>();
            state = AddComponent<Components.AnimState>();
        }

        public override void Load()
        {
            base.Load();

            health.Max = 100;
            health.Current = 100;
        }

        /// <summary>
        /// The current faction. Typically used by the AI to determine enemies
        /// </summary>
        /// <remarks>0 is any/no faction</remarks>
        int Faction { get; set; } = 0;

        Vector2 lastVelocity = Vector2.Zero;
        public override void Think(GameTime Time)
        {
            //todo: move to physics
            var vel = Velocity;

            if (vel == lastVelocity)
                vel = Vector2.Lerp(vel, Vector2.Zero, 10 * (float)Time.ElapsedGameTime.TotalSeconds);
            if (System.Math.Abs(vel.X) < 0.01f)
                vel.X = 0;
            if (System.Math.Abs(vel.Y) < 0.01f)
                vel.Y = 0;

            var lSq = vel.LengthSquared();
            if (lSq > MaxSpeed * MaxSpeed)
                vel = (vel / (float)System.Math.Sqrt(lSq)) * MaxSpeed;

            Velocity = vel;
            lastVelocity = Velocity;

            base.Think(Time);
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

        #endregion
    }
}
