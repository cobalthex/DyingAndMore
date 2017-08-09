using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DyingAndMore.Game.Entities
{
    class InputController : Controller
    {
        public PlayerIndex player = PlayerIndex.One;

        public override void Think(System.TimeSpan DeltaTime)
        {
            if (actor.Map.TraceLine(actor.Position, actor.Direction, out var hit))
                actor.Map.DrawLine(actor.Position, actor.Position + actor.Direction * hit.distance, Color.Aquamarine);

            var d = Vector2.Zero;
            if (Takai.Input.InputState.IsButtonDown(Keys.A))
                d -= Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.W))
                d -= Vector2.UnitY;
            if (Takai.Input.InputState.IsButtonDown(Keys.D))
                d += Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.S))
                d += Vector2.UnitY;

            if (Takai.Input.InputState.IsButtonDown(Takai.Input.MouseButtons.Left))
                actor.FireWeapon();

            actor.Accelerate(d);

            var dir = Takai.Input.InputState.PolarMouseVector;
            dir.Normalize();
            actor.Direction = dir;
        }
    }
}
