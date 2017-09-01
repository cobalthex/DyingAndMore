using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

using FxList = System.Collections.Generic.List<Takai.Game.IGameEffect>;

namespace DyingAndMore.Game
{
    class Game : MapView
    {
        Entities.ActorInstance player = null;
        Entities.Controller lastController = null;

        FxList testEffect;

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

            map.renderSettings.DrawBordersAroundNonDrawingEntities = true;

            renderSettingsConsole = GeneratePropSheet(map.renderSettings, DefaultFont, DefaultColor);
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
                Color = Color.PaleGreen
            });

            testEffect = Takai.Data.Cache.Load<FxList>("defs/effects/test.fx.tk");
        }

        protected override void OnMapChanged(System.EventArgs e)
        {
            Map.updateSettings = Takai.Game.Map.UpdateSettings.Game;
            Map.renderSettings = Takai.Game.Map.RenderSettings.Default;

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

            crapDisplay.Text = $"TimeScale:{Map.TimeScale}\nZoom:{Map.ActiveCamera.Scale}";
            crapDisplay.AutoSize();

            Vector2 worldMousePos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

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

            if (InputState.IsPress(MouseButtons.Left))
            {
                var pos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
                Map.Spawn(testEffect, pos, Vector2.Normalize(player.Position - pos));
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
