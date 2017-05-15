using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai;

namespace DyingAndMore.Game
{
    class Game : Takai.Runtime.GameState
    {
        public Takai.Game.Map map;

        Entities.Actor player = null;
        Entities.Controller lastController = null;

        Takai.Graphics.BitmapFont fnt;

        SpriteBatch sbatch;

        public Game() : base(false, false) { }

        void StartMap()
        {
            map.updateSettings = Takai.Game.MapUpdateSettings.Game;
            map.renderSettings.drawBordersAroundNonDrawingEntities = false;
            map.renderSettings.drawGrids = false;
            map.renderSettings.drawTriggers = false;

            var plyr = from ent in map.FindEntitiesByType<Entities.Actor>(true)
                       where ((Entities.Actor)ent).Faction == Entities.Factions.Player
                       select ent;

            player = plyr.FirstOrDefault() as Entities.Actor;
            map.ActiveCamera = new Takai.Game.Camera(player);

            var testScript = new Scripts.TestScript()
            {
                totalTime = System.TimeSpan.FromSeconds(10),
                victim = player
            };
            map.AddScript(testScript);

            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");
        }

        public override void Load()
        {
            fnt = AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/rct2.bfnt");

            if (map == null)
            {
                map = new Takai.Game.Map(GraphicsDevice);
                using (var s = new System.IO.FileStream("data/maps/test.map.tk", System.IO.FileMode.Open))
                    map = Takai.Game.Map.Load(s);
            }

            sbatch = new SpriteBatch(GraphicsDevice);

            StartMap();

            pt1 = new Takai.Game.ParticleType()
            {
                Graphic = new Takai.Graphics.Sprite(
                    Takai.AssetManager.Load<Texture2D>("Textures/Particles/Trail.png"),
                    8,
                    8,
                    1,
                    System.TimeSpan.FromMilliseconds(100),
                    Takai.Graphics.TweenStyle.Sequential,
                    true
                )
            };
            pt1.Graphic.CenterOrigin();

            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(1, 1));

            pt1.BlendMode = BlendState.AlphaBlend;
            pt1.Color = new Takai.Game.ValueCurve<Color>(curve, Color.White, Color.Red);
            pt1.Scale = new Takai.Game.ValueCurve<float>(curve, 1, 2);
            pt1.Speed = new Takai.Game.ValueCurve<float>(curve, 100, 0);

            if (player?.PrimaryWeapon is Weapons.Gun wpn)
            {
                wpn.Speed = 0;
            }
        }
        Takai.Game.ParticleType pt1, pt2;

        public override void Update(GameTime time)
        {
            map.ActiveCamera.Viewport = GraphicsDevice.Viewport.Bounds;

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.O))
            {
                var ofd = new System.Windows.Forms.OpenFileDialog()
                {
                    SupportMultiDottedExtensions = true,
                    Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*",
                    InitialDirectory = System.IO.Path.GetDirectoryName(map.File),
                    FileName = System.IO.Path.GetFileName(map.File)
                };
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var stream = ofd.OpenFile())
                        map = Takai.Game.Map.Load(stream);
                    StartMap();
                }
                return;
            }

            if (InputState.IsClick(Keys.F1))
            {
                //Takai.Runtime.GameManager.NextState(new Editor.Editor()
                //{
                //    Map = map
                //});
                return;
            }

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Takai.Runtime.GameManager.Exit();
                return;
            }

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

            //possess actors
#if DEBUG
            if (InputState.IsMod(KeyMod.Alt) && InputState.IsPress(MouseButtons.Left))
            {
                var targets = map.FindEntities(map.ActiveCamera.ScreenToWorld(InputState.MouseVector), 5, false);

                foreach (var ent in targets)
                {
                    if (ent is Entities.Actor actor)
                    {
                        Entities.Controller inputCtrl = null;
                        if (player != null)
                        {
                            inputCtrl = player.Controller;
                            player.Controller = lastController;
                        }

                        player = actor;
                        lastController = player.Controller;
                        player.Controller = inputCtrl ?? new Entities.InputController();
                        map.ActiveCamera.Follow = player;
                        break;
                    }
                }
            }
#endif

            var scrollDelta = InputState.ScrollDelta();
            if (InputState.IsMod(KeyMod.Control) && scrollDelta != 0)
            {
                map.TimeScale += System.Math.Sign(scrollDelta) * 0.1f;
            }

            Vector2 worldMousePos = map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn()
            {
                type = pt1,
                angle = new Takai.Game.Range<float>(0, MathHelper.TwoPi),
                position = new Takai.Game.Range<Vector2>(worldMousePos),
                count = new Takai.Game.Range<int>(3, 5),
                lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromMilliseconds(400), System.TimeSpan.FromMilliseconds(800))
            };
            map.Spawn(pspawn);

            map.Update(time);
        }

        public override void Draw(GameTime time)
        {
            Vector2 worldMousePos = map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
            if (player != null)
            {
                var line = map.TraceLine(player.Position, player.Direction, out var hit, 1000);
                //map.DrawLine(player.Position, player.Position + player.Direction * hit.distance, Color.White);
            }

            map.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            //fps
            var sFps = (1 / time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = fnt.MeasureString(sFps);
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            var sDebugInfo =
                $"TimeScale: {map.TimeScale:0.#}x\n" +
                $"Map Debug: {map.debugOut}\n"
            ;

            fnt.Draw(sbatch, sDebugInfo, new Vector2(10), Color.White);

            sbatch.End();
        }
    }
}
