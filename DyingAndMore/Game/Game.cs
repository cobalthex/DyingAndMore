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

        public MapInstance Map
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
        private MapInstance _map;

        public bool IsPaused { get; set; }

        public TimeSpan ElapsedRealTime { get; set; }

        public GameplaySettings GameplaySettings { get; set; } = new GameplaySettings();

        List<PlayerInstance> players;

        Static fpsDisplay;
        Static crapDisplay;
        Static clockDisplay;

        Static renderSettingsConsole;
        Static updateSettingsConsole;
        Static gameplaySettingsConsole;

        Static hudContainer;

        bool isDead = false;
        TimeSpan restartTimer;

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
            ["CompleteMap"] = (ignored) => CompleteMap(),
        };

        public void CompleteMap()
        {
            //todo: display end of game stats

            Game.LoadNextStoryMap();
        }

        void ToggleUI(Static ui)
        {
            if (!ui.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = ui.UserData.GetType();
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
            Current = this; //apply elsewhere?

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

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

            if (updateSettingsConsole != null)
                updateSettingsConsole.UserData = Map.updateSettings;
            if (renderSettingsConsole != null)
                renderSettingsConsole.UserData = Map.renderSettings;

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

            OnResize(EventArgs.Empty);

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

        protected override void OnParentChanged(ParentChangedEventArgs e)
        {
            if (Parent == null)
                return;

            Map.updateSettings.SetGame();
            Map.renderSettings.SetDefault();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            var wx = 0.01f * Size.X;
            var wy = 0.01f * Size.Y;

            for (int i = 0; i < players.Count; ++i)
            {
                var viewport = new Rectangle();
                if (players.Count <= viewportLayouts.Length && i < viewportLayouts[players.Count].Length)
                    viewport = viewportLayouts[players.Count][i];

                //todo: move to resize method (for window resize support too)
                viewport.X = (int)(viewport.X * wx);
                viewport.Y = (int)(viewport.Y * wy);
                viewport.Width = (int)(viewport.Width * wx);
                viewport.Height = (int)(viewport.Height * wy);

                players[i].camera.Viewport = viewport;
            }
        }

        protected override void UpdateSelf(GameTime time)
        {
            ElapsedRealTime += time.ElapsedGameTime;
            clockDisplay.Text = $"{GetClockText(ElapsedRealTime)}\n{GetClockText(Map.ElapsedTime)}x{Map.TimeScale:N1}{(IsPaused ? "\n -- PAUSED --" : "")}";
            clockDisplay.AutoSize();

            if (isDead && time.TotalGameTime > restartTimer)
            {
                Map = Map.Class.Instantiate();
                isDead = false;
                OnParentChanged(new ParentChangedEventArgs(null));
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
            fpsDisplay.AutoSize();

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

                var v = new Vector2(player.camera.Viewport.Width, player.camera.Viewport.Height);
                foreach (var touch in player.inputs.Touches)
                {
                    Takai.Graphics.Primitives2D.DrawLine(spriteBatch, Color.CornflowerBlue,
                        new Vector2(touch.boundsPercent.min.X, touch.boundsPercent.min.Y) * v,
                        new Vector2(touch.boundsPercent.max.X, touch.boundsPercent.min.Y) * v,
                        new Vector2(touch.boundsPercent.max.X, touch.boundsPercent.max.Y) * v,
                        new Vector2(touch.boundsPercent.min.X, touch.boundsPercent.max.Y) * v,
                        new Vector2(touch.boundsPercent.min.X, touch.boundsPercent.min.Y) * v
                    );
                }
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

            if (Map.renderSettings.drawDebugInfo)
            {
                //todo: move text rendering to map (Map.DrawText(text, font))



                //foreach (var sound in Map.Sounds)
                //{
                //    DefaultFont.Draw(
                //        spriteBatch,
                //        $"Vol:{sound.Instance.Volume:N2} Pan:{sound.Instance.Pan:N2} Pit:{sound.Instance.Pitch:N2}",
                //        Map.ActiveCamera.WorldToScreen(sound.Position) + new Vector2(0, 50),
                //        Color.Aquamarine
                //    );
                //}
            }
        }
    }
}

//todo: scale animation speed by charge/discharge time, make charge/discharge range