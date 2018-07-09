using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Reflection;
using Takai.Data;
using Takai.Game;
using Takai.Input;
using Takai.UI;
using Takai;
using System.Collections.Generic;

namespace DyingAndMore.Game
{
    public class GameplaySettings
    {
        public bool isAiEnabled = true;
        public bool isPlayerInputEnabled = true;
    }

    public class GameInstance : MapView
    {
        public static GameInstance Current { get; set; }

        public Game Game { get; set; }
        public TimeSpan ElapsedRealTime { get; set; }

        public GameplaySettings GameplaySettings { get; set; } = new GameplaySettings();

        List<Entities.ActorInstance> players;
        Entities.ActorInstance player = null;
        Entities.Controller lastController = null;

        Static fpsDisplay;
        Static crapDisplay;
        Static clockDisplay;

        TextInput debugConsole;

        Static renderSettingsConsole;
        Static updateSettingsConsole;
        Static gameplaySettingsConsole;

        Static gameHuds;

        public Dictionary<string, CommandAction> GameActions => new Dictionary<string, CommandAction>(StringComparer.OrdinalIgnoreCase)
        {
            ["LoadMap"] = delegate (object map)
            {
                MapInstance inst = null;
                if (map is string mapName)
                {
                    var loaded = Cache.Load(mapName);
                    if (loaded is MapInstance loadedInst)
                        inst = loadedInst;
                    else if (loaded is MapClass loadedClass)
                        inst = loadedClass.Instantiate();
                }
                else if (map is MapClass mapClass)
                    inst = mapClass.Instantiate();
                else
                    inst = map as MapInstance;

                if (inst != null)
                    Map = Game.Map = inst;
            },
            ["LoadNextMap"] = delegate (object ignored)
            {
                Game.LoadNextStoryMap();
                Map = Game.Map;
            },
        };

        void ToggleUI(Static ui)
        {
            if (!ui.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = ui.UserData.GetType().GetTypeInfo();
                foreach (var child in ui.Children)
                    ((CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(ui.UserData);

                AddChild(ui);
                ui.HasFocus = true;
            }
        }

        public GameInstance(Game game)
        {
            if (game?.Map == null)
                throw new ArgumentNullException("There must be a map to play");

            Game = game;

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            AddChild(gameHuds = new Static
            {
                Name = "HUD container",
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.Stretch,
            });

            AddChild(fpsDisplay = new Static
            {
                Name = "FPS",
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
            });
            AddChild(crapDisplay = new Static
            {
                Name = "blah",
                Position = new Vector2(20),
                VerticalAlignment = Alignment.Start,
                HorizontalAlignment = Alignment.End,
                Color = Color.PaleGreen
            });
            AddChild(clockDisplay = new Static
            {
                Name = "clocks",
                Position = new Vector2(0, 20),
                VerticalAlignment = Alignment.Start,
                HorizontalAlignment = Alignment.Middle,
                Color = new Color(1, 1, 1, 0.5f),
            });

            debugConsole = new TextInput
            {
                Name = "debug console",
                Position = new Vector2(20),
                Size = new Vector2(400, 30),
                VerticalAlignment = Alignment.End,
                Font = Cache.Load<Takai.Graphics.BitmapFont>("Fonts/xbox.bfnt"),
            };
            debugConsole.Submit += delegate (object sender, EventArgs e)
            {
                var inp = (TextInput)sender;
                ParseCommand(inp.Text);
                inp.RemoveFromParent();
                inp.Text = String.Empty;
            };

            Map = Game.Map;

            renderSettingsConsole = GeneratePropSheet(Map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Alignment.Middle;
            renderSettingsConsole.UserData = Map.renderSettings;

            updateSettingsConsole = GeneratePropSheet(Map.updateSettings, DefaultFont, DefaultColor);
            updateSettingsConsole.Position = new Vector2(100, 0);
            updateSettingsConsole.VerticalAlignment = Alignment.Middle;
            updateSettingsConsole.UserData = Map.updateSettings;

            gameplaySettingsConsole = GeneratePropSheet(GameplaySettings, DefaultFont, DefaultColor);
            gameplaySettingsConsole.Position = new Vector2(100, 0);
            gameplaySettingsConsole.VerticalAlignment = Alignment.Middle;
            gameplaySettingsConsole.UserData = GameplaySettings;

            Map.renderSettings.drawBordersAroundNonDrawingEntities = true;
        }

        protected override void OnMapChanged(EventArgs e)
        {
            if (Map == null)
                throw new ArgumentNullException("There must be a map to play");

            ElapsedRealTime = TimeSpan.Zero;

            Map.updateSettings = MapInstance.UpdateSettings.Game;
            Map.renderSettings = MapInstance.RenderSettings.Default;

            var players = new List<Entities.ActorInstance>();
            var enemies = new List<Entities.ActorInstance>();

            foreach (var ent in Map.AllEntities)
            {
                if (ent is Entities.ActorInstance actor)
                {
                    if (actor.Controller is Entities.InputController)
                        players.Add(actor);
                    else if (actor.Controller == null)
                        enemies.Add(actor);
                }
            }

            int numPlayers = 1;

            //create extra players if not enough
            for (int i = players.Count; i < numPlayers; ++i)
            {
                //spawn players behind the last
            }

            if (players.Count > 0)
            {
                this.players = players;
                //GameInstance.Current.players = players.GetRange(0, numPlayers);
                //for (int i = numPlayers; i < players.Count; ++i)
                //    Map.Destroy(players[i]);

                player = players[0];
            }

            Map.ActiveCamera = new Camera(player); //todo: resume control

            gameHuds.RemoveAllChildren();
            if (player?.Hud != null)
                gameHuds.AddChild(player.Hud);
        }

        string GetClockText(TimeSpan time)
        {
            return $"{(int)time.TotalHours:D2}:"
                    + $"{time.Minutes:D2}:"
                    + $"{time.Seconds:D2}."
                    + $"{time.Milliseconds:D3}";
        }

        protected override void UpdateSelf(GameTime time)
        {
            ElapsedRealTime += time.ElapsedGameTime;
            clockDisplay.Text = $"{GetClockText(ElapsedRealTime)}\n{GetClockText(Map.ElapsedTime)}x{Map.TimeScale:N1}{(IsPaused ? "\n -- PAUSED --" : "")}";
            clockDisplay.AutoSize();

            if (!IsPaused)
            {
                for (int i = 0; i < (players?.Count ?? 0); ++i)
                {
                    //var region = new Rectangle(GameInstance.Current.players[i].Position.ToPoint(), new Point(1));
                    var region = Map.ActiveCamera.VisibleRegion;
                    region.Inflate(Map.Class.SectorPixelSize, Map.Class.SectorPixelSize);
                    Map.BuildHeuristic((players[i].Position / Map.Class.TileSize).ToPoint(), region, i > 0);
                }
            }

            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";
            fpsDisplay.AutoSize();

            crapDisplay.Text = $"Zoom:{Map.ActiveCamera.Scale:N1}";
            crapDisplay.AutoSize();

            DataModel.Globals["player.health"] = player.CurrentHealth / player.Class.MaxHealth;

            base.UpdateSelf(time);
        }

        void ParseCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            var words = command.Split(' ');
            switch (words[0].ToLowerInvariant())
            {
                case "action":
                    {
                        if (words.Length > 0 && GameActions.TryGetValue(words[1], out var action))
                            action.Invoke(words.Length > 2 ? words[2] : null);
                    }
                    break;

                case "cleanup":
                    {
                        var cleans = MapInstance.CleanupOptions.None;
                        for (int i = 1; i < words.Length; ++i)
                        {
                            switch (words[i])
                            {
                                case "all":
                                    cleans |= MapInstance.CleanupOptions.All;
                                    i = words.Length;
                                    break;

                                case "fluid":
                                case "fluids":
                                    cleans |= MapInstance.CleanupOptions.Fluids;
                                    LogBuffer.Append("Removing all fluids");
                                    break;

                                case "particles":
                                    cleans |= MapInstance.CleanupOptions.Particles;
                                    Takai.LogBuffer.Append("Removing all particles");
                                    break;
                            }
                        }
                        Map.CleanupAll(cleans);
                    }
                    break;

                case "refresh":
                    for (int i = 1; i < words.Length; ++i)
                    {
                        switch (words[i])
                        {
                            case "entities":
                                EntityInstance camFollow = null;
                                var newEnts = new List<EntityInstance>();
                                foreach (var entity in Map.AllEntities)
                                {
                                    var newInst = entity.Class.Instantiate();
                                    newInst.Position = entity.Position;
                                    newInst.Forward = entity.Forward;
                                    newInst.Velocity = entity.Velocity;
                                    newEnts.Add(newInst);

                                    if (Map.ActiveCamera.Follow == entity)
                                        camFollow = newInst;
                                }

                                Map.RemoveAllEntities();
                                foreach (var ent in newEnts)
                                    Map.Spawn(ent);

                                Map.ActiveCamera.Follow = camFollow;

                                break;
                        }
                    }
                    break;

                case "reset":
                    Map = Map.Class.Instantiate();
                    break;

                case "timescale":
                    if (words.Length == 1)
                        Takai.LogBuffer.Append("Time scale: " + Map.TimeScale);
                    else
                        Map.TimeScale = float.Parse(words[1]);
                    break;

                //this should go elsewhere
                case "typeinfo":
                    {
                        if (words.Length < 2)
                            break;

                        ScrollBox s = new ScrollBox()
                        {
                            BackgroundColor = Color.DarkGray,
                            Size = new Vector2(400),
                            HorizontalAlignment = Alignment.Middle,
                            VerticalAlignment = Alignment.Middle
                        };
                        s.Click += delegate (object sender, ClickEventArgs e) { ((Static)sender).RemoveFromParent(); };

                        Dictionary<string, object> type;
                        try
                        {
                            type = Serializer.DescribeType(Serializer.RegisteredTypes[words[1]]);
                        }
                        catch
                        {
                            break;
                        }

                        var info = new Static()
                        {
                            Text = words[1] + "\n" + string.Join("\n", type.Select(e => "    " + e.Key + ": " + e.Value).ToArray()),
                        };
                        info.AutoSize();
                        s.AddChild(info);

                        AddChild(s);
                    }
                    break;

                case "entinfo":
                    {
                        if (words.Length < 2)
                            break;

                        int nextIndex = 1;
                        bool writeFile = false;
                        if (words[1] == "-f")
                        {
                            writeFile = true;
                            ++nextIndex;
                        }

                        ScrollBox s = new ScrollBox()
                        {
                            BackgroundColor = new Color(40, 40, 40),
                            BorderColor = Color.Gray,
                            Size = new Vector2(600),
                            HorizontalAlignment = Alignment.Middle,
                            VerticalAlignment = Alignment.Middle,
                        };
                        s.Click += delegate (object sender, ClickEventArgs e) { ((Static)sender).RemoveFromParent(); };

                        string entInfo = string.Empty;
                        try
                        {
                            using (var writer = new System.IO.StringWriter())
                            {
                                for (int i = nextIndex; i < words.Length; ++i)
                                {
                                    if (i > nextIndex)
                                        writer.WriteLine();
                                    var ent = Map.FindEntityById(int.Parse(words[i]));
                                    Serializer.TextSerialize(writer, ent, 0, false, true);
                                }
                                entInfo = writer.ToString();
                            }
                        }
                        catch
                        {
                            break;
                        }

                        if (writeFile)
                        {
                            System.IO.Directory.CreateDirectory("debug");
                            System.IO.File.WriteAllText("debug\\entinfo.tk", entInfo);
#if WINDOWS
                            System.Diagnostics.Process.Start("debug\\entinfo.tk");
#endif
                        }
                        else
                        {
                            var info = new Static()
                            {
                                Text = entInfo,
                            };
                            info.AutoSize();
                            s.AddChild(info);

                            AddChild(s);
                        }
                    }
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

        System.Diagnostics.Stopwatch swatch = new System.Diagnostics.Stopwatch();
        protected override bool HandleInput(GameTime time)
        {
            Vector2 worldMousePos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            /*if (player != null)
            {
                swatch.Restart();
                var a = player.Position;
                var b = worldMousePos;
                var path = Map.AStarBuildPath(a, b);
                Map.DrawLine(a, b, Color.Black);
                for (int i = 1; i < path.Count; ++i)
                {
                    Map.DrawLine(
                        path[i - 1].ToVector2() * Map.Class.TileSize,
                        path[i].ToVector2() * Map.Class.TileSize,
                        Takai.Util.ColorFromHSL(((float)i / path.Count) * 360, 1, 0.65f)
                    );
                }
                swatch.Stop();
                //Takai.LogBuffer.Append(swatch.ElapsedMilliseconds.ToString());
            }*/

            if (InputState.IsPress(Keys.OemTilde))
            {
                debugConsole.HasFocus = true;
                AddChild(debugConsole);
                return false;
            }

            if (InputState.IsPress(Keys.F1) ||
                InputState.IsAnyPress(Buttons.Start))
            {
                Parent.ReplaceAllChildren(new Editor.Editor(Map));
                return false;
            }

            if (InputState.IsPress(Keys.F2) ||
                InputState.IsAnyPress(Buttons.Back))
            {
                ToggleUI(renderSettingsConsole);
                return false;
            }
            if (InputState.IsPress(Keys.F3) ||
                InputState.IsAnyPress(Buttons.Back))
            {
                ToggleUI(updateSettingsConsole);
                return false;
            }
            if (InputState.IsPress(Keys.F4))
            {
                ToggleUI(gameplaySettingsConsole);
                return false;
            }

#if WINDOWS
            if (InputState.IsPress(Keys.F5))
            {
                using (var sfd = new System.Windows.Forms.SaveFileDialog()
                {
                    Filter = "Dying and More! Saves (*.d2sav)|*.d2sav",
                    RestoreDirectory = true,
                })
                {
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            Map.Save(sfd.FileName);
                        }
                        catch
                        {
                            //todo
                        }
                    }
                }
                return false;
            }
#endif

            if (InputState.IsPress(Keys.Escape))
                IsPaused = !IsPaused;

            var scrollDelta = InputState.ScrollDelta();
            if (scrollDelta != 0)
            {
                if (InputState.IsMod(KeyMod.Control))
                    Map.TimeScale += Math.Sign(scrollDelta) * 0.1f;
                else if (InputState.IsMod(KeyMod.Alt))
                    Map.ActiveCamera.Rotation += Math.Sign(scrollDelta) * MathHelper.PiOver4;
                else
                    Map.ActiveCamera.Scale += Math.Sign(scrollDelta) * 0.1f;
            }

#if DEBUG
            {
                if (InputState.IsButtonDown(MouseButtons.Middle))
                {
                    Map.ActiveCamera._DebugSetTransform(true, -Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(Map.ActiveCamera.Transform)));
                }
            }
#endif

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Takai.Runtime.IsExiting = true;
                return false;
            }

            //possess actors
#if DEBUG
            if (InputState.IsMod(KeyMod.Alt) && InputState.IsPress(MouseButtons.Left))
            {
                var targets = Map.FindEntitiesInRegion(worldMousePos, 5);

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

            var particleCount = 0;
            foreach (var ptype in Map.Particles)
                particleCount += ptype.Value.Count;

            DefaultFont.Draw(spriteBatch,
                $"Total entities:{Map.AllEntities.Count()}" +
                $"\nActive entities:{Map.UpdateStats.updatedEntities}" +
                $"\nTotal particles:{particleCount}" +
                $"\nVisible fluids:A={Map.RenderStats.visibleActiveFluids} I={Map.RenderStats.visibleInactiveFluids}" +
                $"\nTrail points:{Map.RenderStats.trailPointCount}",
                new Vector2(20),
                Color.Orange
            );

            if (Map.renderSettings.drawDebugInfo)
            {
                foreach (var ent in Map.EnumerateVisibleEntities())
                {
                    var pos = Map.ActiveCamera.WorldToScreen(ent.Position) + new Vector2(ent.Radius / 2 * Map.ActiveCamera.Scale);

                    var sb = new System.Text.StringBuilder();
                    sb.Append($"{ent.Id}: {string.Join(",", ent.ActiveAnimations)}\n");

                    if (ent is Entities.ActorInstance actor)
                    {
                        sb.Append($"`f76{actor.CurrentHealth} {string.Join(",", actor.ActiveAnimations)}`x\n");
                        if (actor.Weapon is Weapons.GunInstance gun)
                            sb.Append($"`bcf{gun.AmmoCount} {gun.State} {gun.Charge:N2}`x\n");
                        if (actor.Controller is Entities.AIController ai)
                        {
                            sb.Append($"`ad4{ai.Target}");
                            for (int i = 0; i < ai.ChosenBehaviors.Length; ++i)
                            {
                                if (ai.ChosenBehaviors[i] != null)
                                {
                                    sb.Append("\n");
                                    sb.Append(ai.ChosenBehaviors[i].ToString());
                                }
                            }
                            sb.Append("`x\n");
                        }
                    }
                    DefaultFont.Draw(spriteBatch, sb.ToString(), pos, Color.White);

                    //draw trigger names
                }

                foreach (var sound in Map.Sounds)
                {
                    DefaultFont.Draw(
                        spriteBatch,
                        $"Vol:{sound.Instance.Volume:N2} Pan:{sound.Instance.Pan:N2} Pit:{sound.Instance.Pitch:N2}",
                        Map.ActiveCamera.WorldToScreen(sound.Position) + new Vector2(0, 50),
                        Color.Aquamarine
                    );
                }
            }
        }
    }
}

//todo: scale animation speed by charge/discharge time, make charge/discharge range