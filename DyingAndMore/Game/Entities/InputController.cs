using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DyingAndMore.Game.Entities
{
    class InputController : Controller
    {
        public PlayerIndex player = PlayerIndex.One;

        public override void Think(System.TimeSpan DeltaTime)
        {
            var color = Color.MediumAquamarine;

            var trace = actor.Map.Trace(actor.Position, actor.Forward, 0, actor);
            if (trace.entity != null)
                color = Color.Tomato;

            actor.Map.DrawLine(actor.Position, actor.Position + actor.Forward * trace.distance, color);

            var d = Vector2.Zero;
            if (Takai.Input.InputState.IsButtonDown(Keys.W))
                d -= Vector2.UnitY;
            if (Takai.Input.InputState.IsButtonDown(Keys.A))
                d -= Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.S))
                d += Vector2.UnitY;
            if (Takai.Input.InputState.IsButtonDown(Keys.D))
                d += Vector2.UnitX;

            if (Takai.Input.InputState.IsButtonDown(Takai.Input.MouseButtons.Left))
                actor.Weapon?.Charge();
            if (Takai.Input.InputState.IsClick(Takai.Input.MouseButtons.Left))
                actor.Weapon?.Reset();

            actor.Accelerate(d);

            var dir = Takai.Input.InputState.PolarMouseVector;
            dir.Normalize();
            actor.Forward = dir;
        }
    }
}
