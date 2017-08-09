using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Game
{
    class Game : MapView
    {
        Entities.ActorInstance player = null;
        Entities.Controller lastController = null;

        Static fpsDisplay;
        Static crapDisplay;

        Static renderSettingsConsole;
        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(Takai.Game.Map.RenderSettings);
                foreach (var child in renderSettingsConsole.Children)
                    ((CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);

                AddChild(renderSettingsConsole);
            }
        }

        public Game(Takai.Game.Map map)
        {
            Map = map ?? throw new System.ArgumentNullException("There must be a map to play");

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            testParticles = new Takai.Game.ParticleType()
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
            testParticles.Graphic.CenterOrigin();

            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(1, 1));

            testParticles.BlendMode = BlendState.AlphaBlend;
            testParticles.Color = new Takai.Game.ValueCurve<Color>(curve, Color.White, Color.Transparent);
            testParticles.Scale = new Takai.Game.ValueCurve<float>(curve, 1, 2);
            testParticles.Speed = new Takai.Game.ValueCurve<float>(curve, 100, 0);

            map.renderSettings |= Takai.Game.Map.RenderSettings.DrawBordersAroundNonDrawingEntities;

            //renderSettingsConsole = GeneratePropSheet(map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsConsole = new Static();
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Alignment.Middle;

            AddChild(fpsDisplay = new Static()
            {
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
            });
            AddChild(crapDisplay = new Static()
            {
                Position = new Vector2(20),
                VerticalAlignment = Alignment.Start,
                HorizontalAlignment = Alignment.End,
            });
        }
        Takai.Game.ParticleType testParticles;

        protected override void OnMapChanged(System.EventArgs e)
        {
            Map.updateSettings = Takai.Game.MapUpdateSettings.Game;
            Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawBordersAroundNonDrawingEntities;
            //Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawGrids;
            Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawTriggers;

            var possibles = Map.FindEntitiesByClassName("player");
            if (possibles.Count > 0)
            {
                player = possibles[0] as Entities.ActorInstance;
                player.Controller = new Entities.InputController();
            }
            Map.ActiveCamera = new Takai.Game.Camera(player);

            var testScript = new Scripts.TestScript()
            {
                totalTime = System.TimeSpan.FromSeconds(10),
                victim = player
            };
            Map.AddScript(testScript);

            //Map.Tiles[0, 0] = 9;

            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");
        }
        
        protected override void UpdateSelf(GameTime time)
        {
            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";
            fpsDisplay.AutoSize();

            crapDisplay.Text = $"TimeScale:{Map.TimeScale}";
            crapDisplay.AutoSize();

            Vector2 worldMousePos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            Takai.Game.ParticleSpawn pspawn = new Takai.Game.ParticleSpawn()
            {
                type = testParticles,
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

            var scrollDelta = InputState.ScrollDelta();
            if (scrollDelta != 0)
            {
                if (InputState.IsMod(KeyMod.Control))
                    Map.TimeScale += System.Math.Sign(scrollDelta) * 0.1f;
                else
                    Map.ActiveCamera.Scale += System.Math.Sign(scrollDelta) * 0.1f;
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
                    if (ent is Entities.ActorInstance actor)
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
                        Map.ActiveCamera.Follow = player;
                        break;
                    }
                }
                return false;
            }
#endif

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            DefaultFont.Draw(spriteBatch, $"Total entities:{Map.TotalEntitiesCount}", new Vector2(20), Color.Orange);
        }
    }
}
