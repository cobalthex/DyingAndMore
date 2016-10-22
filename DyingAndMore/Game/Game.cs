using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Takai.Input;

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

        public Game() : base(false, false) { }

        void StartMap()
        {
            map.updateSettings = Takai.Game.MapUpdateSettings.Game;

            var plyr = from ent in map.FindEntitiesByType<Entities.Actor>(true)
                       where ((Entities.Actor)ent).Faction == Entities.Factions.Player
                       select ent;

            player = plyr.FirstOrDefault() as Entities.Actor;
            camera.Follow = player;
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

            sbatch = new SpriteBatch(GraphicsDevice);

            camera = new Takai.Game.Camera(map, player);
            camera.MoveSpeed = 800;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;
            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");

            StartMap();

            pt1 = new Takai.Game.ParticleType();
            pt1.Graphic = new Takai.Graphics.Sprite(
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
            pt1.Color = new Takai.Game.ValueCurve<Color>(curve, Color.White, Color.Red);
            pt1.Scale = new Takai.Game.ValueCurve<float>(curve, 1, 2);
            pt1.Speed = new Takai.Game.ValueCurve<float>(curve, 100, 0);
        }
        Takai.Game.ParticleType pt1, pt2;

        public override void Update(GameTime Time)
        {
            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.O))
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

            if (InputState.IsClick(Keys.F1))
            {
                Takai.States.StateManager.NextState(new Editor.Editor() { map = map, camera = new Takai.Game.Camera(map, camera.ActualPosition) { Viewport = camera.Viewport } });
                return;
            }

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Takai.States.StateManager.Exit();
                return;
            }

            if (InputState.IsPress(Keys.F2))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (InputState.IsPress(Keys.F3))
                map.debugOptions.showOnlyReflections ^= true;

            if (InputState.IsPress(Keys.F5))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Create))
                    map.SaveState(stream);
            }
            if (InputState.IsPress(Keys.F9))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Open))
                    map.LoadState(stream);
                StartMap();
            }

            var scrollDelta = InputState.ScrollDelta();
            if (InputState.IsMod(KeyMod.Control) && scrollDelta != 0)
            {
                map.TimeScale += System.Math.Sign(scrollDelta) * 0.1f;
            }

            camera.Update(Time);

            Vector2 worldMousePos = camera.ScreenToWorld(InputState.MouseVector);

            Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn();
            pspawn.type = pt1;
            pspawn.angle = new Takai.Game.Range<float>(0, MathHelper.TwoPi);
            pspawn.position = new Takai.Game.Range<Vector2>(worldMousePos);
            pspawn.count = new Takai.Game.Range<int>(3, 5);
            pspawn.lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromMilliseconds(400), System.TimeSpan.FromMilliseconds(800));
            //map.Spawn(pspawn);
        }

        public override void Draw(GameTime Time)
        {
            Vector2 worldMousePos = camera.ScreenToWorld(InputState.MouseVector);
            float line;
            var ent = map.TraceLine(player.Position, player.Direction, out line, 1000);
            map.DrawLine(player.Position, player.Position + player.Direction * line, Color.White);

            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            //fps
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = fnt.MeasureString(sFps);
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            var sDebugInfo =
                $"TimeScale: {map.TimeScale:0.#}x"
            ;

            fnt.Draw(sbatch, sDebugInfo, new Vector2(10), Color.White);

            sbatch.End();
        }
    }
}
