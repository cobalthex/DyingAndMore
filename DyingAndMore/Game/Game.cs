using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using Takai.UI;
using Takai.Data;
using Takai.Game;
using Takai.Input;
using Takai;
using System.Collections.Generic;

namespace DyingAndMore.Game
{
    public class GameplaySettings
    {
        public bool isAiEnabled = true;
        public bool isPlayerInputEnabled = true;
    }

    //separate out debug info

    /// <summary>
    /// A display for one local player's camera
    /// </summary>
    public class PlayerInstance
    {
        public Entities.ActorInstance actor;
        public InputMap<Entities.InputAction> inputs;
        public Camera camera;

        public PlayerInstance(Entities.ActorInstance actor, Rectangle viewport)
        {
            inputs = new InputMap<Entities.InputAction>();

            this.actor = actor;
            this.actor.Controller = new Entities.InputController
            {
                Inputs = inputs,
            };

            camera = new Camera(actor)
            {
                Viewport = viewport
            };
        }
    }

    public class GameInstance : Static
    {
        public static GameInstance Current { get; set; }

        /// <summary>
        /// The current game that this game instance is playing
        /// </summary>
        public Game Game
        {
            get => _game;
            set
            {
                if (_game == value)
                    return;

                if (_game != null)
                    _game.MapChanged -= GameMapChanged;

                _game = value;
                if (_game != null)
                    _game.MapChanged += GameMapChanged;
            }
        }
        private Game _game;

        public MapBaseInstance Map
        {
            get => _map;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Map cannot be null");
                if (Map == value)
                    return;

                _map = value;
                OnMapChanged();
            }
        }
        private MapBaseInstance _map;

        public bool IsPaused { get; set; }

        public TimeSpan ElapsedRealTime { get; set; }

        public GameplaySettings GameplaySettings { get; set; } = new GameplaySettings();

        List<PlayerInstance> players;

        Static fpsDisplay;
        Static crapDisplay;
        Static clockDisplay;

        TabPanel settingsConsole;
        Static renderSettingsPane;
        Static updateSettingsPane;
        Static gameplaySettingsPane;

        Static hudContainer;

        bool isDead = false;
        TimeSpan restartTimer;

        public Dictionary<string, CommandAction> GameActions => new Dictionary<string, CommandAction>(StringComparer.OrdinalIgnoreCase)
        {
            ["LoadMap"] = delegate (object map)
            {
                MapBaseInstance inst = null;
                if (map is string mapName)
                {
                    var loaded = Cache.Load(mapName);
                    if (loaded is MapBaseInstance loadedInst)
                        inst = loadedInst;
                    else if (loaded is MapBaseClass loadedClass)
                        inst = loadedClass.Instantiate();
                }
                else if (map is MapBaseClass mapClass)
                    inst = mapClass.Instantiate();
                else
                    inst = map as MapBaseInstance;

                if (inst != null)
                    Map = Game.Map = inst;
            },
            ["CompleteMap"] = (ignored) => CompleteMap(),
            ["Cleanup"] = delegate (object ignored)
            {
                Map.CleanupAll(MapBaseInstance.CleanupOptions.All);
            },
        };

        public void CompleteMap()
        {
            //todo: display end of game stats

            Game.LoadNextStoryMap();
        }

        public GameInstance(Game game)
        {
            if (game?.Map == null)
                throw new ArgumentNullException("There must be a map to play");

            game.Map.PackageMap("Maps/test.d2map");

            Game = game;
            Current = this; //apply elsewhere?

            AddChild(hudContainer = new Static
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
                Name = "stuff display",
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

            Map = game.Map;

            AddChild(settingsConsole = new TabPanel
            {
                Name = "Settings Console",
                IsEnabled = false,
                Position = new Vector2(100, 200),
                Padding = new Vector2(10, 5),
                Margin = 10,
                BackgroundColor = new Color(40, 40, 40),
                BorderColor = Color.Gray
            });

            renderSettingsPane = GeneratePropSheet(Map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsPane.Name = "Render Settings";
            settingsConsole.AddChild(renderSettingsPane);

            updateSettingsPane = GeneratePropSheet(Map.updateSettings, DefaultFont, DefaultColor);
            updateSettingsPane.Name = "Update Settings";
            settingsConsole.AddChild(updateSettingsPane);

            gameplaySettingsPane = GeneratePropSheet(GameplaySettings, DefaultFont, DefaultColor);
            gameplaySettingsPane.Name = "Gameplay Settings";
            settingsConsole.AddChild(gameplaySettingsPane);

            Map.renderSettings.drawBordersAroundNonDrawingEntities = true;
            Map.Class.DebugFont = DefaultFont;

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            On(ParentChangedEvent, OnParentChanged);
        }

        private void GameMapChanged(object sender, MapChangedEventArgs e)
        {
            Map = ((Game)sender).Map;
        }

        protected void OnMapChanged()
        {
            if (Map.Class != Game.Map.Class)
                Game = new Game { Map = Map }; //todo: proper map reset support

            ElapsedRealTime = TimeSpan.Zero;

            hudContainer.RemoveAllChildren();

            updateSettingsPane?.BindTo(Map.updateSettings);
            renderSettingsPane?.BindTo(Map.renderSettings);

            var possiblePlayers = new List<Entities.ActorInstance>();

            foreach (var ent in Map.AllEntities)
            {
                if (ent is Entities.ActorInstance actor && actor.IsAlliedWith(Entities.Factions.Player))
                    possiblePlayers.Add(actor);
            }

            int numPlayers = 1;

            //create extra players if not enough
            for (int i = possiblePlayers.Count; i < numPlayers; ++i)
            {
                //spawn players behind the last
            }

            players = new List<PlayerInstance>(possiblePlayers.Count);
            for (int i = 0; i < possiblePlayers.Count; ++i)
            {
                players.Add(new PlayerInstance(possiblePlayers[i], new Rectangle()));

                if (possiblePlayers[i].Hud != null)
                    hudContainer.AddChild(possiblePlayers[i].Hud);
                if (possiblePlayers[i].Weapon?.Hud != null)
                    hudContainer.AddChild(possiblePlayers[i].Hud);
            }

            players[0].inputs = Cache.Load<InputMap<Entities.InputAction>>("Player1.input.tk", "Config");
            ((Entities.InputController)players[0].actor.Controller).Inputs = players[0].inputs;
        }

        /// <summary>
        /// The player layouts, organized by [# of players][player index], each value is out of 100 (%)
        /// </summary>
        //too: multiple layouts per player count (horizontal vs vertical layout, etc)
        static readonly Rectangle[][] viewportLayouts = new Rectangle[][]
        {
            new []
            {
                new Rectangle(0, 0, 0, 0),
            },
            new []
            {
                new Rectangle(0, 0, 100, 100)
            },
            new []
            {
                new Rectangle(0,  0, 100, 50),
                new Rectangle(0, 50, 100, 50),
            },
            new []
            {
                new Rectangle( 0,  0, 100, 40),
                new Rectangle( 0, 40,  50, 60),
                new Rectangle(50, 40,  50, 60),
            },
            new []
            {
                new Rectangle( 0,  0, 50, 50),
                new Rectangle(50,  0, 50, 50),
                new Rectangle( 0, 50, 50, 50),
                new Rectangle(50, 50, 50, 50),
            },
        };

        string GetClockText(TimeSpan time)
        {
            return $"{(int)time.TotalHours:D2}:"
                    + $"{time.Minutes:D2}:"
                    + $"{time.Seconds:D2}."
                    + $"{time.Milliseconds:D3}";
        }

        protected UIEventResult OnParentChanged(Static sender, UIEventArgs e)
        {
            if (Parent == null)
                return UIEventResult.Continue;

            Map.updateSettings.SetGame();
            Map.renderSettings.SetDefault();
            updateSettingsPane?.BindTo(Map.updateSettings);
            renderSettingsPane?.BindTo(Map.renderSettings);
            return UIEventResult.Handled;
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            //todo: should this go in MeasureOverride?

            var wx = 0.01f * availableSize.X;
            var wy = 0.01f * availableSize.Y;

            if (players != null)
            {
                for (int i = 0; i < players.Count; ++i)
                {
                    var viewport = new Rectangle();
                    if (players.Count <= viewportLayouts.Length && i < viewportLayouts[players.Count].Length)
                        viewport = viewportLayouts[players.Count][i];

                    viewport.X = (int)(viewport.X * wx);
                    viewport.Y = (int)(viewport.Y * wy);
                    viewport.Width = (int)(viewport.Width * wx);
                    viewport.Height = (int)(viewport.Height * wy);

                    players[i].camera.Viewport = viewport;
                }
            }
            base.ReflowOverride(availableSize);
        }

        protected override void UpdateSelf(GameTime time)
        {
            ElapsedRealTime += time.ElapsedGameTime;
            clockDisplay.Text = $"{GetClockText(ElapsedRealTime)}\n{GetClockText(Map.ElapsedTime)}x{Map.TimeScale:N1}{(IsPaused ? "\n -- PAUSED --" : "")}";

            if (isDead && time.TotalGameTime > restartTimer)
            {
                Map = Map.Class.Instantiate();
                isDead = false;
                OnParentChanged(this, new ParentChangedEventArgs(this, null));
                Reflow();
                return;
            }

            bool isPlayerInputEnabled = Runtime.HasFocus && GameplaySettings.isPlayerInputEnabled;

            if (!IsPaused)
            {
                Map.BeginUpdate();

                var allDead = true;
                //todo: this doesn't support networking
                for (int i = 0; i < players.Count; ++i)
                {
                    //var region = new Rectangle(GameInstance.Current.players[i].Position.ToPoint(), new Point(1));
                    var region = players[i].camera.VisibleRegion;
                    region.Inflate(Map.Class.SectorPixelSize, Map.Class.SectorPixelSize);

                    Map.BuildHeuristic((players[i].actor.Position / Map.Class.TileSize).ToPoint(), region, i > 0);

                    players[i].camera.Update(time);
                    Map.MarkRegionActive(players[i].camera);

                    if (isPlayerInputEnabled)
                    {
                        players[i].inputs.Update((PlayerIndex)i, players[i].camera.Viewport);

                        if (players[i].inputs.CurrentInputs.TryGetValue(Entities.InputAction.ZoomCamera, out var zoom))
                            players[i].camera.Scale += 0.1f * zoom;
                    }

                    allDead &= !players[i].actor.IsAlive;
                }

                if (!isDead && allDead)
                    restartTimer = time.TotalGameTime + TimeSpan.FromSeconds(2);
                isDead = allDead;

                Map.Update(time);
            }

            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";

            base.UpdateSelf(time);
        }

        protected void SwitchToEditor()
        {
            if (Editor.Editor.Current == null)
                Editor.Editor.Current = new Editor.Editor(Map);
            else
                Editor.Editor.Current.Map = Map;

            Parent.ReplaceAllChildren(Editor.Editor.Current);
        }

        System.Diagnostics.Stopwatch swatch = new System.Diagnostics.Stopwatch();
        protected override bool HandleInput(GameTime time)
        {
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

            if (InputState.IsPress(Keys.F1) ||
                InputState.IsAnyPress(Buttons.Start))
            {
                SwitchToEditor();
                return false;
            }

            if (InputState.IsPress(Keys.F2) ||
                InputState.IsAnyPress(Buttons.Back))
            {
                settingsConsole.IsEnabled ^= true;
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

            //todo
            var scrollDelta = InputState.ScrollDelta();
            if (scrollDelta != 0)
            {
                if (InputState.IsMod(KeyMod.Control))
                    Map.TimeScale += Math.Sign(scrollDelta) * 0.1f;
                //else if (InputState.IsMod(KeyMod.Alt))
                //    Map.ActiveCamera.Rotation += Math.Sign(scrollDelta) * MathHelper.PiOver4;
                //else
                //    Map.ActiveCamera.Scale += Math.Sign(scrollDelta) * 0.1f;
            }

            //#if DEBUG
            //            {
            //                if (InputState.IsButtonDown(MouseButtons.Middle))
            //                {
            //                    Map.ActiveCamera._DebugSetTransform(true, -Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(Map.ActiveCamera.Transform)));
            //                }
            //            }
            //#endif

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Runtime.IsExiting = true;
                return false;
            }

            //possess actors
#if DEBUG && false //todo: fix
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

            foreach (var player in players)
            {
                Map.Draw(player.camera);
                Takai.Graphics.Primitives2D.DrawLine(spriteBatch, Color.Gray,
                    new Vector2(player.camera.Viewport.Right, player.camera.Viewport.Top),
                    new Vector2(player.camera.Viewport.Right, player.camera.Viewport.Bottom),
                    new Vector2(player.camera.Viewport.Left, player.camera.Viewport.Bottom)
                );
                DefaultFont.Draw(spriteBatch,
                    $"Health:{player.actor.CurrentHealth}/{player.actor.Class.MaxHealth}\n" +
                    $"Weapon:{player.actor.Weapon}",
                    new Vector2(player.camera.Viewport.Left + 10, player.camera.Viewport.Top + 10),
                    Color.Cyan
                );

                //var v = new Vector2(player.camera.Viewport.Width, player.camera.Viewport.Height);
                //foreach (var touch in player.inputs.Touches)
                //{
                //    Takai.Graphics.Primitives2D.DrawLine(spriteBatch, Color.CornflowerBlue,
                //        new Vector2(touch.boundsPercent.min.X, touch.boundsPercent.min.Y) * v,
                //        new Vector2(touch.boundsPercent.max.X, touch.boundsPercent.min.Y) * v,
                //        new Vector2(touch.boundsPercent.max.X, touch.boundsPercent.max.Y) * v,
                //        new Vector2(touch.boundsPercent.min.X, touch.boundsPercent.max.Y) * v,
                //        new Vector2(touch.boundsPercent.min.X, touch.boundsPercent.min.Y) * v
                //    );
                //}
            }

            var particleCount = 0;
            foreach (var ptype in Map.Particles)
                particleCount += ptype.Value.Count;

            DefaultFont.Draw(spriteBatch,
                $"Total entities:{Map.AllEntities.Count()}" +
                $"\nActive entities:{Map.UpdateStats.updatedEntities}" +
                $"\nTotal particles:{particleCount}" +
                $"\nVisible fluids:A={Map.RenderStats.visibleActiveFluids} I={Map.RenderStats.visibleInactiveFluids}" +
                $"\nTrail points:{Map.RenderStats.trailPointCount}",
                new Vector2(20, 200),
                Color.Orange
            );
        }
    }
}

//todo: scale animation speed by charge/discharge time, make charge/discharge range