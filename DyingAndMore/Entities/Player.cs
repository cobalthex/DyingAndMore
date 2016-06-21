using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Entities
{
    class Player : Actor
    {
        public Player()
        {
            AlwaysActive = true;
        }

        public override void Load()
        {
            var idle = new Takai.Graphics.Graphic
            (
                Takai.AssetManager.Load<Texture2D>("Textures/Player.png"),
                new Point(48, 48),
                null,
                null,
                2,
                System.TimeSpan.FromMilliseconds(150),
                Takai.AnimationOptions.Loop | Takai.AnimationOptions.StartImmediately,
                Takai.Graphics.TweenStyle.None
            );
            idle.CenterOrigin();

            var dying = idle.Clone();
            dying.clipRect = new Rectangle(0, 96, 48, 48);
            dying.isLooping = false;
            dying.frameLength = System.TimeSpan.FromSeconds(1);
            dying.tween = Takai.Graphics.TweenStyle.Consecutive;
            
            state.States.Add("idle", idle);
            state.States.Add("dying", dying);
            state.Current = "idle";

            Radius = 25;
        }

        System.TimeSpan lastShotTime = System.TimeSpan.Zero;
        System.TimeSpan shotDelay = System.TimeSpan.FromMilliseconds(100);
        public override void Think(GameTime Time)
        {
            var vel = Velocity;

            float ac = MoveForce;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
                vel.Y -= ac;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
                vel.X -= ac;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
                vel.Y += ac;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
                vel.X += ac;
            Velocity = vel;

            if (Takai.Input.InputCatalog.IsKeyClick(Microsoft.Xna.Framework.Input.Keys.K))
                state.Current = "dying";

            if (Takai.Input.InputCatalog.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && Time.TotalGameTime > lastShotTime + shotDelay)
            {
                lastShotTime = Time.TotalGameTime;
                Map.SpawnEntity<Projectile>(Position + ((Radius + 5) * Direction), Direction, Direction * 500);
            }


            base.Think(Time);
        }
    }
}
