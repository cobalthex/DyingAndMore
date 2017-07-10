using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai;

namespace DyingAndMore.Game
{
    class Game : Takai.UI.MapView
    {
        Entities.ActorInstance player = null;
        Entities.Controller lastController = null;

        Takai.UI.Static renderSettingsConsole;
        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(Takai.Game.Map.RenderSettings);
                foreach (var child in renderSettingsConsole.Children)
                    ((Takai.UI.CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);

                AddChild(renderSettingsConsole);
            }
        }

        public Game(Takai.Game.Map map)
        {
            Map = map ?? throw new System.ArgumentNullException("There must be a map to play");

            HorizontalAlignment = Takai.UI.Alignment.Stretch;
            VerticalAlignment = Takai.UI.Alignment.Stretch;

            pt1 = new Takai.Game.ParticleType()
            {
                Graphic = new Takai.Graphics.Sprite(
                    Takai.AssetManager.Load<Texture2D>("Textures/Particles/Blood.png"),
                    10,
                    10,
                    6,
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
            pt1.Color = new Takai.Game.ValueCurve<Color>(curve, Color.White, Color.Aquamarine);
            pt1.Scale = new Takai.Game.ValueCurve<float>(curve, 1, 2);
            pt1.Speed = new Takai.Game.ValueCurve<float>(curve, 100, 0);

            map.renderSettings |= Takai.Game.Map.RenderSettings.DrawBordersAroundNonDrawingEntities;

            //renderSettingsConsole = GeneratePropSheet(map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsConsole = new Takai.UI.Static();
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Takai.UI.Alignment.Middle;
        }
        Takai.Game.ParticleType pt1, pt2;

        protected override void OnMapChanged(System.EventArgs e)
        {
            Map.updateSettings = Takai.Game.MapUpdateSettings.Game;
            Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawBordersAroundNonDrawingEntities;
            Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawGrids;
            Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawTriggers;

            //var plyr = from ent in Map.FindEntitiesByClass(true)
            //           where ((Entities.Actor)ent).Faction == Entities.Factions.Player
            //           select ent;
            //player = plyr.FirstOrDefault() as Entities.Actor;

            Map.ActiveCamera = new Takai.Game.Camera(player);

            var testScript = new Scripts.TestScript()
            {
                totalTime = System.TimeSpan.FromSeconds(10),
                victim = player
            };
            Map.AddScript(testScript);

            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");
        }

        protected override void UpdateSelf(GameTime time)
        {
            var scrollDelta = InputState.ScrollDelta();
            if (InputState.IsMod(KeyMod.Control) && scrollDelta != 0)
            {
                Map.TimeScale += System.Math.Sign(scrollDelta) * 0.1f;
            }

            Vector2 worldMousePos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn()
            {
                type = pt1,
                angle = new Takai.Game.Range<float>(0, MathHelper.TwoPi),
                position = new Takai.Game.Range<Vector2>(worldMousePos),
                count = new Takai.Game.Range<int>(3, 5),
                lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromMilliseconds(400), System.TimeSpan.FromMilliseconds(800))
            };
            Map.Spawn(pspawn);

            base.UpdateSelf(time);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.F1))
            {
                Parent.ReplaceAllChildren(new Editor.Editor(Map));
                return false;
            }

            if (InputState.IsPress(Keys.F2))
            {
                ToggleRenderSettingsConsole();
                return false;
            }

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.O))
            {
                var ofd = new System.Windows.Forms.OpenFileDialog()
                {
                    SupportMultiDottedExtensions = true,
                    Filter = "Map (*.Map.tk)|*.Map.tk|All Files (*.*)|*.*",
                    InitialDirectory = System.IO.Path.GetDirectoryName(Map.File),
                    FileName = System.IO.Path.GetFileName(Map.File)
                };
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var stream = ofd.OpenFile())
                        Map = Takai.Game.Map.Load(stream);
                }
                return false;
            }

            if (InputState.IsClick(Keys.F1))
            {
                //Takai.Runtime.GameManager.NextState(new Editor.Editor()
                //{
                //    Map = Map
                //});
                return false;
            }

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Takai.Runtime.IsExiting = true;
                return false;
            }

            if (InputState.IsPress(Keys.F5))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Create))
                    Map.SaveState(stream);
                return false;
            }
            if (InputState.IsPress(Keys.F9))
            {
                using (var stream = new System.IO.FileStream("test.sav.tk", System.IO.FileMode.Open))
                    Map.LoadState(stream);
                OnMapChanged(System.EventArgs.Empty);
                return false;
            }

            //possess actors
#if DEBUG
            if (InputState.IsMod(KeyMod.Alt) && InputState.IsPress(MouseButtons.Left))
            {
                var targets = Map.FindEntities(Map.ActiveCamera.ScreenToWorld(InputState.MouseVector), 5, false);

                foreach (var ent in targets)
                {
                    //if (ent is Entities.ActorInstance actor)
                    //{
                    //    Entities.Controller inputCtrl = null;
                    //    if (player != null)
                    //    {
                    //        inputCtrl = player.Controller;
                    //        player.Controller = lastController;
                    //    }

                    //    player = actor;
                    //    lastController = player.Controller;
                    //    player.Controller = inputCtrl ?? new Entities.InputController();
                    //    Map.ActiveCamera.Follow = player;
                    //    break;
                    //}
                }
                return false;
            }
#endif

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            Vector2 worldMousePos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
            if (player != null)
            {
                var line = Map.TraceLine(player.Position, player.Direction, out var hit, 1000);
                //Map.DrawLine(player.Position, player.Position + player.Direction * hit.distance, Color.White);
            }
        }
    }
}
