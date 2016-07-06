using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Entities
{
    class Player : Actor
    {
        Takai.Game.BlobType blobType, blobType2;

        public Components.Weapon weapon;

        public Player()
        {
            AlwaysActive = true;
            weapon = AddComponent<Components.Weapon>();
        }

        public override void Load()
        {
            var idle = new Takai.Graphics.Graphic
            (
                Takai.AssetManager.Load<Texture2D>("Textures/Player.png"),
                48,
                48,
                2,
                System.TimeSpan.FromMilliseconds(150),
                true,
                true
            );
            Radius = 24;
            idle.CenterOrigin();

            var dying = idle.Clone();
            dying.ClipRect = new Rectangle(0, 96, 48, 48);
            dying.IsLooping = false;
            dying.FrameTime = System.TimeSpan.FromSeconds(1);
            dying.Tween = Takai.Graphics.TweenStyle.Sequentially;
            
            state.States.Add("idle", idle);
            state.States.Add("dying", dying);
            state.Current = "idle";

            blobType = new Takai.Game.BlobType();
            blobType.Drag = 2.2f;
            blobType.Radius = 10;
            blobType.Texture = Takai.AssetManager.Load<Texture2D>("Textures/ablob.png");
            blobType.Reflection = Takai.AssetManager.Load<Texture2D>("Textures/ablobr.png");

            blobType2 = new Takai.Game.BlobType();
            blobType2.Drag = 1.8f;
            blobType2.Radius = 10;
            blobType2.Texture = Takai.AssetManager.Load<Texture2D>("Textures/bblob.png");
            blobType2.Reflection = Takai.AssetManager.Load<Texture2D>("Textures/bblobr.png");

            weapon.template = new Projectile();
            weapon.speed = 400;
        }
        
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
            if (Takai.Input.InputCatalog.IsKeyClick(Microsoft.Xna.Framework.Input.Keys.I))
                state.Current = "idle";
            if (Takai.Input.InputCatalog.IsKeyClick(Microsoft.Xna.Framework.Input.Keys.N))
                IsPhysical = !IsPhysical;

            if (Sprite.IsFinished())
                IsEnabled = false;

            if (Takai.Input.InputCatalog.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                weapon.Fire();

            //if (Takai.Input.InputCatalog.MouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && Time.TotalGameTime > lastShotTime + shotDelay)
            //{
            //    lastShotTime = Time.TotalGameTime;
            //    bool isLsh = Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
            //    Map.SpawnBlob(isLsh ? blobType2 : blobType, Position + ((Radius + 30) * Direction), Direction * 100);
            //}

            Map.DebugLine(Position, Position + Direction * 1000, Color.GreenYellow);

            base.Think(Time);
        }
    }
}
