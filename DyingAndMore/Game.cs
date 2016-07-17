using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    class Game : Takai.States.State
    {
        Entities.Actor player;
        Entities.Actor ent;

        Takai.Game.Camera camera;

        Takai.Graphics.BitmapFont fnt;
        string debugText = "";

        SpriteBatch sbatch;
        
        Takai.Game.Map map;

        public Game() : base(Takai.States.StateType.Full) { }

        public override void Load()
        {
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/Debug.bfnt");

            map = new Takai.Game.Map(GraphicsDevice);
            using (var s = new System.IO.FileStream("data/maps/test.csv", System.IO.FileMode.Open))
                map.ReadTiles(s);
            map.TilesImage = Takai.AssetManager.Load<Texture2D>("Textures/Tiles2.png");
            map.TileSize = 48;
            map.BuildMask(map.TilesImage, true);
            map.BuildSectors();

            map.DebugFont = fnt;

            player = map.SpawnEntity<Entities.Actor>(new Vector2(100), Vector2.UnitX, Vector2.Zero, false);
            player.Name = "player";
            player.MoveForce = 600;
            player.MaxSpeed = 400;
            player.Sprite = Takai.Graphics.Graphic.FromFile("test.gfx.tk");
            //player.Sprite = new Takai.Graphics.Graphic
            //(
            //    Takai.AssetManager.Load<Texture2D>("Textures/Player.png"),
            //    48,
            //    48,
            //    2,
            //    System.TimeSpan.FromMilliseconds(100),
            //    Takai.Graphics.TweenStyle.Overlap,
            //    true
            //);
            //player.Sprite.CenterOrigin();
            player.Radius = player.Sprite.Width / 2;
            player.States.Add("idle", player.Sprite);
            player.CurrentState = "idle";
            
            var gun = new Weapons.Gun();
            gun.projectile = new Entities.Projectile();
            gun.projectile.Load();
            gun.speed = 800;
            gun.shotDelay = System.TimeSpan.FromMilliseconds(50);
            player.primaryWeapon = gun;

            var blobber = new Weapons.BlobGun();
            blobber.blob = new Takai.Game.BlobType();
            blobber.blob.Radius = 54;
            blobber.blob.Drag = 1.5f;
            blobber.blob.Texture = Takai.AssetManager.Load<Texture2D>("Textures/bblob.png");
            blobber.blob.Reflection = Takai.AssetManager.Load<Texture2D>("Textures/bblobr.png");
            blobber.speed = 100;
            player.altWeapon = blobber;

            //todo: change Load/Unload to OnSpawn/Destroy and call then

            ent = map.SpawnEntity<Entities.Actor>(new Vector2(40), Vector2.UnitX, Vector2.Zero);
            var sprite = new Takai.Graphics.Graphic
            (
                Takai.AssetManager.Load<Texture2D>("Textures/InfectedCell.png"),
                64,
                64,
                new Rectangle(0, 32, 256, 96),
                4,
                System.TimeSpan.FromMilliseconds(100),
                Takai.Graphics.TweenStyle.Overlap,
                true
            );
            ent.Radius = sprite.Width / 2;
            sprite.CenterOrigin();
            ent.Sprite = sprite;

            sbatch = new SpriteBatch(GraphicsDevice);
            
            map.debugOptions.showProfileInfo = true;
            map.debugOptions.showEntInfo = true;
            
            camera = new Takai.Game.Camera(map, player);
            camera.MoveSpeed = 800;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;
            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");

            var tex = Takai.AssetManager.Load<Texture2D>("Textures/SparkParticle.png");
            map.AddDecal(tex, new Vector2(200, 100));
            map.AddDecal(tex, new Vector2(300, 120), 0, 2);
        }
        
        public override void Update(GameTime Time)
        {
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.Q))
                Takai.States.StateManager.Exit();

            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F1))
                map.debugOptions.showProfileInfo ^= true;
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F2))
                map.debugOptions.showEntInfo ^= true;
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F3))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F4))
                map.debugOptions.showOnlyReflections ^= true;

            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F5))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Create))
                    map.WriteState(stream);
            }
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F9))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Open))
                    map.ReadState(stream);
                player = map.FindEntityByName("player") as Entities.Actor;
                if (camera.Follow != null)
                    camera.Follow = player;
            }

            if (Takai.Input.InputCatalog.IsKeyPress(Keys.N))
                camera.Follow = (camera.Follow == null ? player : null);

            var d = Vector2.Zero;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.A))
                d -= Vector2.UnitX;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.W))
                d -= Vector2.UnitY;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.D))
                d += Vector2.UnitX;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.S))
                d += Vector2.UnitY;

            player.Move(d);

            if (player.primaryWeapon != null && Takai.Input.InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                player.primaryWeapon.Fire(Time, player);
            if (player.altWeapon != null && Takai.Input.InputCatalog.MouseState.RightButton == ButtonState.Pressed)
                player.altWeapon.Fire(Time, player);

            var dir = Takai.Input.InputCatalog.MouseState.Position.ToVector2();
            dir -= new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2;
            dir.Normalize();
            player.Direction = dir;

            if (camera.Follow == null)
            {
                if (d != Vector2.Zero)
                    d.Normalize();
                camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds;
            }
            debugText = camera.Position.ToString();

            camera.Update(Time);
        }
        
        public override void Draw(Microsoft.Xna.Framework.GameTime Time)
        {
            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);
            
            sbatch.End();
            
            ent.OutlineColor = Color.Transparent;
        }
    }
}
