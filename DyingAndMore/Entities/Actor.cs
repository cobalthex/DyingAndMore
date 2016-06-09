using Microsoft.Xna.Framework;
using Takai.Graphics;

namespace DyingAndMore.Entities
{
    class Actor : Takai.Game.Entity
    {
        public Vector2 Velocity { get; set; } = Vector2.Zero;
        public float MaxSpeed { get; set; } = 10;
        public float MoveForce { get; set; } = 60;

        /// <summary>
        /// The current faction. Typically used by the AI to determine enemies
        /// </summary>
        int Faction { get; set; }

        Vector2 lastVelocity = Vector2.Zero;
        public override void Think(GameTime Time)
        {
            var vel = Velocity;

            if (vel == lastVelocity)
                vel = Vector2.Lerp(vel, Vector2.Zero, 8 * (float)Time.ElapsedGameTime.TotalSeconds);
            if (System.Math.Abs(vel.X) < 0.01f)
                vel.X = 0;
            if (System.Math.Abs(vel.Y) < 0.01f)
                vel.Y = 0;

            var lSq = vel.LengthSquared();
            if (lSq > MaxSpeed * MaxSpeed)
                vel = (vel / (float)System.Math.Sqrt(lSq)) * MaxSpeed;
            Velocity = vel;

            Position += Velocity;
            lastVelocity = Velocity;

            base.Think(Time);
        }
    }
}
