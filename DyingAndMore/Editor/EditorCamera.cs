using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class EditorCamera : Takai.Game.Camera
    {
        public override void Update(GameTime time)
        {
            if (InputState.IsButtonDown(MouseButtons.Middle))
            {
                MoveTo(Position - Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(Transform)));
            }
            else
            {
                var d = Vector2.Zero;
                if (InputState.IsButtonDown(Keys.A) || InputState.IsButtonDown(Keys.Left))
                    d -= Vector2.UnitX;
                if (InputState.IsButtonDown(Keys.W) || InputState.IsButtonDown(Keys.Up))
                    d -= Vector2.UnitY;
                if (InputState.IsButtonDown(Keys.D) || InputState.IsButtonDown(Keys.Right))
                    d += Vector2.UnitX;
                if (InputState.IsButtonDown(Keys.S) || InputState.IsButtonDown(Keys.Down))
                    d += Vector2.UnitY;

                if (d != Vector2.Zero)
                {
                    d.Normalize();
                    d = d * MoveSpeed * (float)time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                    Position += Vector2.TransformNormal(d, Matrix.Invert(Transform));
                }
            }
            if (InputState.HasScrolled())
            {
                var delta = (Scale * InputState.ScrollDelta()) / 1024f;
                if (InputState.IsButtonDown(Keys.LeftShift))
                {
                    Rotation += delta;
                }
                else
                {
                    Scale += delta;
                    if (System.Math.Abs(Scale - 1) < 0.1f) //snap to 100% when near
                        Scale = 1;
                    else
                        Scale = MathHelper.Clamp(Scale, 0.1f, 2f);
                }
            }

            Scale = MathHelper.Clamp(Scale, 0.1f, 10f); //todo: make ranges global and move to some game settings

            base.Update(time); //camera follow?
        }
    }
}
