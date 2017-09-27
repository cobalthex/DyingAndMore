﻿using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

using System;
using Takai.Game;

namespace DyingAndMore.Game
{
    public enum GameDifficulty
    {
        Trivial,
        Easy,
        Hard,
        Impossible,
        Custom
    }

    public struct GameInstance
    {
        //move into Game?

        public static GameInstance Current { get; set; }

        public GameDifficulty difficulty;
        public bool allowFriendlyFire;

        //campaign
    }

    class Game : MapView
    {
        Entities.ActorInstance player = null;
        Entities.Controller lastController = null;

        Static fpsDisplay;
        Static crapDisplay;

        TextInput debugConsole;

        Takai.Graphics.BitmapFont tinyFont;

        EffectsClass testEffect;

        Static renderSettingsConsole;
        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(Map.RenderSettings);
                foreach (var child in renderSettingsConsole.Children)
                    ((CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);

                AddChild(renderSettingsConsole);
            }
        }

        public Game(Map map)
        {
            Map = map ?? throw new ArgumentNullException("There must be a map to play");

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

            debugConsole = new TextInput()
            {
                Position = new Vector2(20),
                Size = new Vector2(400, 30),
                VerticalAlignment = Alignment.End,
                Font = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/xbox.bfnt"),
            };
            debugConsole.Submit += delegate (object sender, EventArgs e)
            {
                var inp = (TextInput)sender;
                ParseCommand(inp.Text);
                inp.RemoveFromParent();
                inp.Text = String.Empty;
            };

            tinyFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UITiny.bfnt");

            testEffect = Takai.Data.Cache.Load<EffectsClass>("Defs/Effects/Damage.fx.tk");

            GameInstance.Current = new GameInstance();
        }

        class BulletTimeScript : Script
        {
            public Curve timeSlow = new Curve();
            TimeSpan totalTime = TimeSpan.FromSeconds(5);
            TimeSpan elapsedTime;

            public BulletTimeScript() : base("Test")
            {
                timeSlow.Keys.Add(new CurveKey(0, 1));
                timeSlow.Keys.Add(new CurveKey(0.5f, 0.25f));
                timeSlow.Keys.Add(new CurveKey(0.95f, 0.1f));
                timeSlow.Keys.Add(new CurveKey(1, 1));
            }

            public override void Step(TimeSpan deltaTime)
            {
                deltaTime = TimeSpan.FromMilliseconds(deltaTime.TotalMilliseconds / Map.TimeScale);
                if (elapsedTime < totalTime)
                {
                    var pct = elapsedTime.TotalMilliseconds / totalTime.TotalMilliseconds;
                    Map.TimeScale = timeSlow.Evaluate((float)pct);
                    elapsedTime += deltaTime;
                }
                else
                {
                    //Map.TimeScale = 1;
                    //destroy this script
                }
            }
        }

        protected override void OnMapChanged(EventArgs e)
        {
            Map.updateSettings = Map.UpdateSettings.Game;
            Map.renderSettings = Map.RenderSettings.Default;

            Map.CleanupAll(
                Map.CleanupOptions.DeadEntities |
                Map.CleanupOptions.Particles
            );

            var possibles = Map.FindEntitiesByClassName("player");
            if (possibles.Count > 0)
            {
                player = possibles[0] as Entities.ActorInstance;
                player.Controller = new Entities.InputController();
            }
            Map.ActiveCamera = new Camera(player);

            var testScript = new BulletTimeScript();
            //Map.AddScript(testScript);

            //Map.Tiles[0, 0] = 9;

            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");

            foreach (var entity in Map.AllEntities)
            {
                if (entity != player)
                {
                    entity.Parent = player;
                    entity.RelativePosition = new Vector2(100);
                    break;
                }
            }
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

        void ParseCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            var words = command.Split(' ');
            switch (words[0].ToLowerInvariant())
            {
                case "cleanup":
                    {
                        var cleans = Map.CleanupOptions.None;
                        for (int i = 1; i < words.Length; ++i)
                        {
                            switch (words[i])
                            {
                                case "all":
                                    cleans |= Map.CleanupOptions.All;
                                    i = words.Length;
                                    break;

                                case "fluid":
                                case "fluids":
                                    cleans |= Map.CleanupOptions.Fluids;
                                    Takai.LogBuffer.Append("Removing all fluids");
                                    break;

                                case "particles":
                                    cleans |= Map.CleanupOptions.Particles;
                                    Takai.LogBuffer.Append("Removing all particles");
                                    break;
                            }
                        }
                        Map.CleanupAll(cleans);
                    }
                    break;

                case "timescale":
                    if (words.Length == 1)
                        Takai.LogBuffer.Append("Time scale: " + Map.TimeScale);
                    else
                        Map.TimeScale = float.Parse(words[1]);
                    break;

                //this should go elsewhere
                case "makedef":
                    using (var stream = new System.IO.StreamWriter("defs.tk"))
                    {
                        for (int i = 1; i < words.Length; ++i)
                        {
                            if (Takai.Data.Serializer.RegisteredTypes.TryGetValue(words[i], out var ty))
                            {
                                Takai.Data.Serializer.TextSerialize(stream, Activator.CreateInstance(ty));
                                stream.WriteLine('\n');
                            }
                        }
                    }
                    System.Diagnostics.Process.Start("defs.tk");
                    break;

                case "exit":
                case "quit":
                    Takai.Runtime.IsExiting = true;
                    break;

                default:
                    Takai.LogBuffer.Append("Unkown command: " + words[0]);
                    break;
            }
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(MouseButtons.Left))
            {
                var fx = testEffect.Create();
                fx.Position = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
                fx.Direction = fx.Position - player.Position;
                Map.Spawn(fx);
                return false;
            }

            if (InputState.IsPress(Keys.OemTilde))
            {
                debugConsole.HasFocus = true;
                AddChild(debugConsole);
                return false;
            }

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
                    Map.TimeScale += Math.Sign(scrollDelta) * 0.1f;
                else
                    Map.ActiveCamera.Scale += Math.Sign(scrollDelta) * 0.1f;
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
                        Map = Map.Load(stream);
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

            foreach (var ent in Map.ActiveEnts)
            {
                if (ent is Entities.ActorInstance actor)
                {
                    var pos = Vector2.Transform(actor.Position, Map.ActiveCamera.Transform)
                              - new Vector2(ent.Radius * 1.5f * Map.ActiveCamera.Scale);

                    tinyFont.Draw(spriteBatch, actor.CurrentHealth.ToString(), pos, Color.Tomato);
                    if (actor.Weapon is Weapons.GunInstance gun)
                    {
                        pos.Y += 10;
                        tinyFont.Draw(spriteBatch, gun.CurrentAmmo.ToString(), pos, Color.LightSteelBlue);
                    }
                }
            }
        }
    }
}
