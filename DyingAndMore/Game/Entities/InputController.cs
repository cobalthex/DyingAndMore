using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DyingAndMore.Game.Entities
{
    class InputController : Controller
    {
        public PlayerIndex player;

        public override void Think(GameTime Time)
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

            if (actor.PrimaryWeapon != null && Takai.Input.InputState.IsButtonDown(Takai.Input.MouseButtons.Left))
                actor.PrimaryWeapon.Fire(Time, actor);
            if (actor.AltWeapon != null && Takai.Input.InputState.IsButtonDown(Takai.Input.MouseButtons.Right))
                actor.AltWeapon.Fire(Time, actor);

            //var dir = Takai.Input.InputState.MouseVector;
            //dir -= new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2;
            //dir.Normalize();
            //actor.Direction = dir;
        }
    }
}
