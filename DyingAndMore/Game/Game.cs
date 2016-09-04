using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Game
{
    class Game : Takai.States.State
    {
        Entities.Actor player;
        Entities.Actor testEnt;

        Takai.Game.Camera camera;

        Takai.Graphics.BitmapFont fnt;
        string debugText = "";

        SpriteBatch sbatch;
        
        Takai.Game.Map map;

        public Game() : base(Takai.States.StateType.Full) { }

        public override void Load()
        {
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UITiny.bfnt");

            map = new Takai.Game.Map(GraphicsDevice);
            using (var s = new System.IO.FileStream("data/maps/test.map.tk", System.IO.FileMode.Open))
                map.Load(s);

            //using (var s = new System.IO.FileStream("data/maps/test.csv", System.IO.FileMode.Open))
            //    map.LoadCsv(s);
            //map.TilesImage = Takai.AssetManager.Load<Texture2D>("Textures/Tiles2.png");
            //map.TileSize = 48;
            //map.BuildMask(map.TilesImage, true);
            //map.BuildSectors();

            player = map.FindEntityByName("Player") as Entities.Actor;
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
            
            sbatch = new SpriteBatch(GraphicsDevice);

            camera = new Takai.Game.Camera(map, player);
            camera.MoveSpeed = 800;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;
            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");
        }

        public override void Update(GameTime Time)
        {
            if (Takai.Input.InputState.IsPress(Keys.Q))
                Takai.States.StateManager.Exit();

            if (Takai.Input.InputState.IsPress(Keys.F1))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (Takai.Input.InputState.IsPress(Keys.F2))
                map.debugOptions.showOnlyReflections ^= true;

            if (Takai.Input.InputState.IsPress(Keys.F5))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Create))
                    map.SaveState(stream);
            }
            if (Takai.Input.InputState.IsPress(Keys.F9))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Open))
                    map.LoadState(stream);
                player = map.FindEntityByName("player") as Entities.Actor;
                if (camera.Follow != null)
                    camera.Follow = player;
            }

            if (Takai.Input.InputState.IsPress(Keys.N))
                camera.Follow = (camera.Follow == null ? player : null);

            var d = Vector2.Zero;
            if (Takai.Input.InputState.IsButtonDown(Keys.A))
                d -= Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.W))
                d -= Vector2.UnitY;
            if (Takai.Input.InputState.IsButtonDown(Keys.D))
                d += Vector2.UnitX;
            if (Takai.Input.InputState.IsButtonDown(Keys.S))
                d += Vector2.UnitY;

            if (camera.Follow != null)
                player.Move(d);

            if (player.primaryWeapon != null && Takai.Input.InputState.IsButtonDown(Takai.Input.MouseButtons.Left))
                player.primaryWeapon.Fire(Time, player);
            if (player.altWeapon != null && Takai.Input.InputState.IsButtonDown(Takai.Input.MouseButtons.Right))
                player.altWeapon.Fire(Time, player);

            var dir = Takai.Input.InputState.MouseVector;
            dir -= new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2;
            dir.Normalize();
            player.Direction = dir;

            if (camera.Follow == null)
            {
                if (d != Vector2.Zero)
                    d.Normalize();
                camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds;
            }

            camera.Update(Time);

            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Transparent;
            highlighted = map.FindNearbyEntities(camera.ScreenToWorld(Takai.Input.InputState.MouseVector), 5);
            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Yellow;
        }
        System.Collections.Generic.List<Takai.Game.Entity> highlighted = new System.Collections.Generic.List<Takai.Game.Entity>();

        public override void Draw(GameTime Time)
        {
            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);
            
            sbatch.End();
        }
    }
}
