using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DyingAndMore.Game.Entities
{
    class InputController : Controller
    {
        public PlayerIndex player;

        public override void Think(System.TimeSpan DeltaTime)
        {
            var d = Vector2.Zero;
            if (Takai.Input.InputState.IsButtonDown(Keys.A))
                d -= Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.W))
                d -= Vector2.UnitY;
            if (Takai.Input.InputState.IsButtonDown(Keys.D))
                d += Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.S))
                d += Vector2.UnitY;

            actor.Move(d);

            var dir = Takai.Input.InputState.PolarMouseVector;
            dir.Normalize();
            actor.Direction = dir;
        }
    }
}
