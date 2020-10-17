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
        public bool limitGore = false;
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

        public static string SwitchToEditorCommand = "SwitchToEditor";

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

                var oldMap = _map;
                _map = value;
                OnMapChanged(oldMap);
            }
        }
        private MapInstance _map;

        public bool IsPaused { get; set; }
        float lastGameSpeed;

        public TimeSpan ElapsedRealTime { get; set; }

        public GameplaySettings GameplaySettings { get; set; } = new GameplaySettings();

        List<PlayerInstance> players;

        Static fpsDisplay;
        Static clockDisplay;

        Static settingsConsole;
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
                MapInstance inst = null;
                if (map is string mapName)
                {
                    var loaded = Cache.Load(mapName);
                    if (loaded is MapInstance loadedInst)
                        inst = loadedInst;
                    else if (loaded is MapClass loadedClass)
                        inst = (MapInstance)loadedClass.Instantiate();
                }
                else if (map is MapClass mapClass)
                    inst = (MapInstance)mapClass.Instantiate();
                else
                    inst = map as MapInstance;

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

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            //game.Map.PackageMap("Maps/test.d2map");

            Game = game;
            Current = this; //apply elsewhere?

            AddChild(hudContainer = new Static
            {
                Name = "HUD container",
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.Stretch,
            });

#if DEBUG
            AddChild(fpsDisplay = new Static
            {
                Name = "FPS",
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
            });
            AddChild(clockDisplay = new Static
            {
                Name = "clocks",
                Position = new Vector2(0, 20),
                VerticalAlignment = Alignment.Start,
                HorizontalAlignment = Alignment.Middle,
                Color = new Color(1, 1, 1, 0.5f),
            });
#endif

            CommandActions[SwitchToEditorCommand] = delegate (Static sender, object arg)
            {
                ((GameInstance)sender).SwitchToEditor();
            };

            Map = game.Map;

            var tabs = new TabPanel();

            renderSettingsPane = GeneratePropSheet(Map.renderSettings);
            renderSettingsPane.BackgroundColor = Color.Transparent; //hacky, should fix .Style = null
            renderSettingsPane.Name = "Render Settings";
            renderSettingsPane.HorizontalAlignment = Alignment.Stretch;
            tabs.AddChild(renderSettingsPane);

            updateSettingsPane = GeneratePropSheet(Map.updateSettings);
            updateSettingsPane.BackgroundColor = Color.Transparent;
            updateSettingsPane.Name = "Update Settings";
            updateSettingsPane.HorizontalAlignment = Alignment.Stretch;
            tabs.AddChild(updateSettingsPane);

            gameplaySettingsPane = GeneratePropSheet(GameplaySettings);
            gameplaySettingsPane.BackgroundColor = Color.Transparent;
            gameplaySettingsPane.Name = "Gameplay Settings";
            gameplaySettingsPane.HorizontalAlignment = Alignment.Stretch;
            tabs.AddChild(gameplaySettingsPane);

            var returnToEditor = new Static("Return to editor")
            {
                Style = "Button"
            };
            returnToEditor.EventCommands[ClickEvent] = SwitchToEditorCommand;

            AddChild(settingsConsole = new ScrollBox(new List(returnToEditor, tabs))
            {
                Name = "Settings Console",
                VerticalAlignment = Alignment.Stretch,
                Style = "Frame",
                IsEnabled = false,
            });

            Map.renderSettings.drawBordersAroundNonDrawingEntities = true;
            Map.renderSettings.drawDebugInfo = true;
        }

        private void GameMapChanged(object sender, MapChangedEventArgs e)
        {
            Map = ((Game)sender).Map;
        }

        protected void SelectPlayers()
        {
            hudContainer.RemoveAllChildren();

            var possiblePlayers = new List<Entities.ActorInstance>();

            foreach (var ent in Map.AllEntities)
            {
                if (ent is Entities.ActorInstance actor && actor.Controller is Entities.InputController)
                    possiblePlayers.Add(actor);
            }

            int numPlayers = 1;

            //create extra players if not enough
            for (int i = possiblePlayers.Count; i < numPlayers; ++i)
            {
                //spawn players behind the last
            }

            players = new List<PlayerInstance>(possiblePlayers.Count);
            foreach (var player in possiblePlayers)
            {
                players.Add(new PlayerInstance(player, new Rectangle()));

                //todo: hud container per player

                if (player.Hud != null)
                    hudContainer.AddChild(player.Hud);

#if ANDROID
                var movementInput = new PolarInput
                {
                    ShowNormalizedValue = true,
                    HorizontalAlignment = Alignment.Start,
                    VerticalAlignment = Alignment.End,
                    Position = new Vector2(20),
                    Size = new Vector2(200),
                    Bindings = new List<Binding>
                    {
                        new Binding("Velocity", "NormalizedValue", BindingDirection.TwoWay)
                    }
                };
                var forwardInput = new PolarInput
                {
                    ShowNormalizedValue = true,
                    HorizontalAlignment = Alignment.End,
                    VerticalAlignment = Alignment.End,
                    Position = new Vector2(20),
                    Size = new Vector2(200),
                    Bindings = new List<Binding>
                    {
                        new Binding("Forward", "NormalizedValue", BindingDirection.TwoWay)
                    }
                };
                forwardInput.On(ValueChangedEvent, delegate (Static sender, UIEventArgs e)
                {
                    //todo: this only works when cursor moves, not just held down
                    player.Weapon?.TryUse();
                    return UIEventResult.Handled;
                });
                movementInput.BindTo(player);
                forwardInput.BindTo(player);
                hudContainer.AddChildren(movementInput, forwardInput);
#endif
            }
            if (players.Count > 0)
            {
                players[0].inputs = Cache.Load<InputMap<Entities.InputAction>>("Player1.input.tk", "Config");
                ((Entities.InputController)players[0].actor.Controller).Inputs = players[0].inputs;
            }
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
                new Rectangle( 0,  0, 100,  00),
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

        protected void CreatePlayerViewports()
        {
            if (players == null)
                return;

            var wx = 0.01f * ContentArea.Width;
            var wy = 0.01f * ContentArea.Height;

            for (int i = 0; i < players.Count; ++i)
            {
                var viewport = new Rectangle();
                if (players.Count < viewportLayouts.Length && i < viewportLayouts[players.Count].Length)
                    viewport = viewportLayouts[players.Count][i];

                viewport.X = (int)(viewport.X * wx);
                viewport.Y = (int)(viewport.Y * wy);
                viewport.Width = (int)(viewport.Width * wx);
                viewport.Height = (int)(viewport.Height * wy);

                players[i].camera.Viewport = viewport;
            }
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            CreatePlayerViewports();
            base.ArrangeOverride(availableSize);
        }

        protected void OnMapChanged(MapInstance oldMap)
        {
            if (Map.Class != Game.Map.Class)
                Game = new Game { Map = Map }; //todo: proper map reset support

            Map.LimitGore = GameplaySettings.limitGore;

            ElapsedRealTime = TimeSpan.Zero;

            updateSettingsPane?.BindTo(Map.updateSettings);
            renderSettingsPane?.BindTo(Map.renderSettings);
            SelectPlayers();

            IsPaused = false;
        }

        string GetClockText(TimeSpan time)
        {
            return $"{(int)time.TotalHours:D2}:"
                    + $"{time.Minutes:D2}:"
                    + $"{time.Seconds:D2}."
                    + $"{time.Milliseconds:D3}";
        }

        protected override void OnParentChanged(Static oldParent)
        {
            if (Parent == null)
                return;

            Map.updateSettings.SetGame();
            Map.renderSettings.SetDefault(); //store settings in editor/game and restore
            updateSettingsPane?.BindTo(Map.updateSettings);
            renderSettingsPane?.BindTo(Map.renderSettings);

            SelectPlayers(); //todo: this will sometimes be called w/ OnMapChanged. Find a way to cut this to one
        }

        protected override void UpdateSelf(GameTime time)
        {
            ElapsedRealTime += time.ElapsedGameTime;
#if DEBUG
            clockDisplay.Text = $"{GetClockText(ElapsedRealTime)}\n{GetClockText(Map.ElapsedTime)}x{Map.TimeScale:N1}{(IsPaused ? "\n -- PAUSED --" : "")}";
            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";
#endif

            if (isDead && time.TotalGameTime > restartTimer)
            {
                //todo: this is being called imemediately and not sure why
                isDead = false;
                Map = Cache.Load<MapInstance>(Map.File);
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
                    var region = players[i].camera.VisibleRegion; //todo: clip to some size
                    region.Inflate(Map.Class.SectorPixelSize, Map.Class.SectorPixelSize);

                    Map.BuildHeuristic((players[i].actor.WorldPosition / Map.Class.TileSize).ToPoint(), region, i > 0);

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

            bool isCornered = players[0].actor.IsMaybeCornered();

            var particleCount = 0;
            foreach (var ptype in Map.Particles)
                particleCount += ptype.Value.Count;

            DyingAndMoreGame.DebugDisplay("Total entities", Map.AllEntities.Count());
            DyingAndMoreGame.DebugDisplay("Active entities", Map.UpdateStats.updatedEntities);
            DyingAndMoreGame.DebugDisplay("Total particles", particleCount);
            DyingAndMoreGame.DebugDisplay("Trail points", Map.RenderStats.trailPointCount);
            DyingAndMoreGame.DebugDisplay("Visible fluids (active)", Map.RenderStats.visibleActiveFluids);
            DyingAndMoreGame.DebugDisplay("Visible fluids (inactive)", Map.RenderStats.visibleInactiveFluids);

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

            //todo: should be a flag if can go to editor
            if (InputState.IsPress(Keys.F1) ||
                InputState.IsAnyPress(Buttons.Start))
            {
                //call SwitchToEditor command?
                SwitchToEditor();
                return false;
            }

#if DEBUG
            if (InputState.IsPress(Keys.F2) ||
                InputState.IsAnyPress(Buttons.Back))
            {
                settingsConsole.IsEnabled ^= true;
                return false;
            }
#endif

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

#if DEBUG
            if (IsPaused)
            {
                //per viewport?
                if (players.Count > 0)
                {
                    if (InputState.IsButtonDown(MouseButtons.Middle))
                        players[0].camera.MoveTo(players[0].camera.ActualPosition - 
                            InputState.MouseDelta() / players[0].camera.ActualScale);

                    var delta = InputState.ScrollDelta();
                    if (delta != 0)
                        players[0].camera.ActualScale += Math.Sign(InputState.ScrollDelta()) * 0.2f;
                }
            }
#endif

            if (InputState.IsPress(Keys.Escape))
            {
                if (InputState.IsMod(KeyMod.Shift))
                {
                    if (Map.TimeScale == 0)
                        Map.TimeScale = lastGameSpeed;
                    else
                    {
                        lastGameSpeed = Map.TimeScale;
                        Map.TimeScale = 0;
                    }
                }    
                else
                    IsPaused = !IsPaused;
            }

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

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);

#if DEBUG
            var drawText = new Takai.Graphics.DrawTextOptions("", DebugFont, DebugTextStyle, Color.HotPink, Vector2.Zero);
#endif

            foreach (var player in players)
            {
#if DEBUG
                foreach (var ent in Map.ActiveEntities)
                {
                    if (ent is Entities.ActorInstance actor && actor._Class.MaxHealth > 0 && ent.IsAlive)
                    {
                        var pos = player.camera.WorldToScreen(ent.WorldPosition - new Vector2(0, ent.Radius + 16));
                        DrawHealthBar(context.spriteBatch, pos, actor.CurrentHealth, actor._Class.MaxHealth);

                        if (Map.renderSettings.drawDebugInfo)
                        {
                            drawText.position = player.camera.WorldToScreen(ent.WorldPosition) + new Vector2(5);
                            drawText.text = ent.GetDebugInfo();
                            context.textRenderer.Draw(drawText);
                        }

                        Map.DrawLine(actor.WorldPosition + actor.WorldForward * 50, actor.WorldPosition, Color.Black, 50, actor._Class.FieldOfView / 2);
                    }
                }
#endif

                Map.Draw(player.camera);

#if DEBUG
                //todo: viewport should be localized
                DrawRect(context.spriteBatch, Color.Gray, player.camera.Viewport);
                
                //Font.Draw(spriteBatch,
                //    $"Health:{player.actor.CurrentHealth}/{player.actor._Class.MaxHealth}\n" +
                //    $"Weapon:{player.actor.Weapon}",
                //    new Vector2(player.camera.Viewport.Left + 10, player.camera.Viewport.Top + 10),
                //    Color.Cyan
                //);

#endif

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
        }

        protected void DrawHealthBar(SpriteBatch spriteBatch, Vector2 screenPosition, float health, float maxHealth)
        {
            //todo: use DrawFill/Rect

            var pc = (maxHealth == 0 ? 0 : MathHelper.Clamp(health, 0, maxHealth) / maxHealth);

            var rect = new Rectangle((int)screenPosition.X - 30, (int)screenPosition.Y - 3, 60, 6);
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, Color.Tomato, rect);
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.Brown, rect);
            rect.Width = (int)(rect.Width * pc);
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, health > maxHealth ? Color.LightSteelBlue : Color.LawnGreen, rect);
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.Teal, rect);
        }
    }
}

//todo: scale animation speed by charge/discharge time, make charge/discharge range