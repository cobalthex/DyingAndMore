using Microsoft.Xna.Framework;

namespace DyingAndMore.Entities
{
    class Player : Actor
    {
        public override void Think(GameTime Time)
        {
            var vel = Velocity;

            float ac = MoveForce * (float)Time.ElapsedGameTime.TotalSeconds;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
                vel.Y -= ac;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
                vel.X -= ac;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
                vel.Y += ac;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
                vel.X += ac;

            Velocity = vel;

            base.Think(Time);
        }
    }
}
