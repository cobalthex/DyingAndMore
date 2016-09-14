using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Game
{
    class Game : Takai.States.State
    {
        public Takai.Game.Map map;
        public Takai.Game.Camera camera;

        Entities.Actor player;

        Takai.Graphics.BitmapFont fnt;
        string debugText = "";

        SpriteBatch sbatch;

        public Game() : base(Takai.States.StateType.Full) { }

        void StartMap()
        {
            player = map.FindEntityByName("Player") as Entities.Actor;
            if (player != null)
                player.CurrentState = Takai.Game.EntState.Idle;
        }

        public override void Load()
        {
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UITiny.bfnt");

            if (map == null)
            {
                map = new Takai.Game.Map(GraphicsDevice);
                using (var s = new System.IO.FileStream("data/maps/test.map.tk", System.IO.FileMode.Open))
                    map.Load(s);
            }

            StartMap();
            
            var gun = new Weapons.Gun();
            gun.Projectile = new Entities.Projectile();
            gun.Speed = 800;
            gun.shotDelay = System.TimeSpan.FromMilliseconds(250);
            player.primaryWeapon = gun;

            var blobber = new Weapons.BlobGun();
            blobber.blob = new Takai.Game.BlobType();
            blobber.blob.Radius = 54;
            blobber.blob.Drag = 1.5f;
            blobber.blob.Texture = Takai.AssetManager.Load<Texture2D>("Textures/Blobs/blood.png");
            blobber.blob.Reflection = Takai.AssetManager.Load<Texture2D>("Textures/Blobs/blood.r.png");
            blobber.speed = 100;
            player.altWeapon = blobber;

            //todo: change Load/Unload to OnSpawn/Destroy and call then

            sbatch = new SpriteBatch(GraphicsDevice);

            camera = new Takai.Game.Camera(map, player);
            camera.MoveSpeed = 800;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;
            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");

            pt1 = new Takai.Game.ParticleType();
            pt1.Graphic = new Takai.Graphics.Graphic(
                Takai.AssetManager.Load<Texture2D>("Textures/Particles/Star.png"),
                32,
                32,
                4,
                System.TimeSpan.FromMilliseconds(100),
                Takai.Graphics.TweenStyle.Sequential,
                true
            );
            pt1.Graphic.CenterOrigin();

            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(1, 1));

            pt1.BlendMode = BlendState.AlphaBlend;
            pt1.Color = new Takai.Game.ValueCurve<Color>(curve, Color.White, Color.Black);
            pt1.Scale = new Takai.Game.ValueCurve<float>(curve, 1, 5);
            pt1.Speed = new Takai.Game.ValueCurve<float>(curve, 100, 0);
        }
        Takai.Game.ParticleType pt1, pt2;

        public override void Update(GameTime Time)
        {
            if (Takai.Input.InputState.IsButtonDown(Keys.LeftControl) && Takai.Input.InputState.IsPress(Keys.O))
            {
                var ofd = new System.Windows.Forms.OpenFileDialog();
                ofd.SupportMultiDottedExtensions = true;
                ofd.Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*";
                ofd.InitialDirectory = System.IO.Path.GetDirectoryName(map.File);
                ofd.FileName = System.IO.Path.GetFileName(map.File);
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var stream = ofd.OpenFile())
                        map.Load(stream, true);
                    StartMap();
                }
                return;
            }

            if (Takai.Input.InputState.IsClick(Keys.F1))
            {
                Takai.States.StateManager.NextState(new Editor.Editor() { map = map, camera = new Takai.Game.Camera(map, camera.ActualPosition) { Viewport = camera.Viewport } });
                return;
            }

            if (Takai.Input.InputState.IsPress(Keys.Q))
            {
                Takai.States.StateManager.Exit();
                return;
            }

            if (Takai.Input.InputState.IsPress(Keys.F2))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (Takai.Input.InputState.IsPress(Keys.F3))
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

            Vector2 worldMousePos = camera.ScreenToWorld(Takai.Input.InputState.MouseVector);

            //Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn();
            //pspawn.type = pt1;
            //pspawn.angle = new Takai.Game.Range<float>(0, MathHelper.TwoPi);
            //pspawn.position = new Takai.Game.Range<Vector2>(worldMousePos);
            //pspawn.count = new Takai.Game.Range<int>(3, 5);
            //pspawn.lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromMilliseconds(400), System.TimeSpan.FromMilliseconds(800));
            //map.Spawn(pspawn);

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
