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
            var dist = 1000f;

            if (actor.Map.ActiveEnts.Count > 1)
            {
                var nextActor = actor.Map.ActiveEnts.GetEnumerator();
                nextActor.MoveNext();
                nextActor.MoveNext();
                if (actor.Map.Intersects(
                    nextActor.Current.Position,
                    nextActor.Current.RadiusSq,
                    actor.Position,
                    actor.Direction,
                    out float t0, out float t1))
                {
                    dist = t0;
                    color = Color.Tomato;
                }
            }

            actor.Map.DrawLine(actor.Position, actor.Position + actor.Direction * dist, color);

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
                actor.FireWeapon();

            actor.Accelerate(d);

            var dir = Takai.Input.InputState.PolarMouseVector;
            dir.Normalize();
            actor.Direction = dir;
        }
    }
}
