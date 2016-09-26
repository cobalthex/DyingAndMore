﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

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
            map.updateSettings = Takai.Game.MapUpdateSettings.Game;

            var plyr = from Entities.Actor ent in map.ActiveEnts
                       where ent.Faction == Entities.Factions.Player
                       select ent;
            
            player = plyr.FirstOrDefault() as Entities.Actor ?? null;
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
                StartMap();
            }
            
            camera.Update(Time);

            Vector2 worldMousePos = camera.ScreenToWorld(Takai.Input.InputState.MouseVector);

            //Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn();
            //pspawn.type = pt1;
            //pspawn.angle = new Takai.Game.Range<float>(0, MathHelper.TwoPi);
            //pspawn.position = new Takai.Game.Range<Vector2>(worldMousePos);
            //pspawn.count = new Takai.Game.Range<int>(3, 5);
            //pspawn.lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromMilliseconds(400), System.TimeSpan.FromMilliseconds(800));
            //map.Spawn(pspawn);

        }

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
