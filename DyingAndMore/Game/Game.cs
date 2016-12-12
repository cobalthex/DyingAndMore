using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Takai.Input;
using System;
using Takai;

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
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/rct2.bfnt");

            if (map == null)
            {
                map = new Takai.Game.Map(GraphicsDevice);
                using (var s = new System.IO.FileStream("data/maps/test.map.tk", System.IO.FileMode.Open))
                    map.Load(s);
            }

            sbatch = new SpriteBatch(GraphicsDevice);

            camera = new Takai.Game.Camera(map, player)
            {
                MoveSpeed = 800,
                Viewport = GraphicsDevice.Viewport.Bounds
            };
            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");

            StartMap();

            pt1 = new Takai.Game.ParticleType()
            {
                Graphic = new Takai.Graphics.Sprite(
                    Takai.AssetManager.Load<Texture2D>("Textures/Particles/Star.png"),
                    32,
                    32,
                    4,
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

            var wpn = player?.PrimaryWeapon as Weapons.Gun;
            if (wpn != null)
            {
                wpn.Speed = 0;
            }

            sm.States.Add("A", new TestSMState());
            sm.States.Add("B", new TestSMState());
            sm.States.Add("C", new TestSMState());
            sm.Transition(null, "A");
            sm.Transition("A", "B");
            sm.Transition("B", "C");
            sm.Transition("C", "A");
        }
        Takai.Game.ParticleType pt1, pt2;

        class TestSMState : Takai.IState
        {
            protected TimeSpan elapsed;

            public bool IsLooping { get; set; }

            public bool HasFinished()
            {
                return elapsed > TimeSpan.FromSeconds(3);
            }

            public void Start()
            {
                elapsed = TimeSpan.Zero;
            }

            public void Update(TimeSpan DeltaTime)
            {
                elapsed += DeltaTime;
            }
        }

        Takai.StateMachine<string, TestSMState> sm = new Takai.StateMachine<string, TestSMState>();

        public override void Update(GameTime Time)
        {
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

            Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn()
            {
                type = pt1,
                angle = new Takai.Game.Range<float>(0, MathHelper.TwoPi),
                position = new Takai.Game.Range<Vector2>(worldMousePos),
                count = new Takai.Game.Range<int>(3, 5),
                lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromMilliseconds(400), System.TimeSpan.FromMilliseconds(800))
            };
            //map.Spawn(pspawn);

            sm.Update(Time.ElapsedGameTime);
        }

        public override void Draw(GameTime Time)
        {
            Vector2 worldMousePos = camera.ScreenToWorld(InputState.MouseVector);
            if (player != null)
            {
                var line = map.TraceLine(player.Position, player.Direction, out var hit, 1000);
                //map.DrawLine(player.Position, player.Position + player.Direction * hit.distance, Color.White);
            }

            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            //fps
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = fnt.MeasureString(sFps);
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            var sDebugInfo =
                $"TimeScale: {map.TimeScale:0.#}x\n" +
                $"    Debug: {map.debugOut}"
            ;

            fnt.Draw(sbatch, sDebugInfo, new Vector2(10), Color.White, true);

            fnt.Draw(sbatch, "States: " + string.Join(",", sm.ActiveStates), new Vector2(100), Color.Aquamarine);

            sbatch.End();
        }
    }
}
