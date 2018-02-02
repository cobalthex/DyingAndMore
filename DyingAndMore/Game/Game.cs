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
    //map spawn configurations? (akin to difficulty)


    public class GameConfiguration
    {
        public string Name { get; set; }
        //spawn settings
        //aggressiveness
        //ammo settings
        public bool AllowFriendlyFire { get; set; }
    }

    public class GameInstance
    {
        //move into Game?
        public TimeSpan ElapsedRealTime { get; set; }

        public static GameInstance Current;

        //Game Campaign
        public GameConfiguration configuration;

        public System.Collections.Generic.List<Entities.ActorInstance> players;

        //campaign
    }

    class Game : MapView
    {
        Entities.ActorInstance player = null;
        Entities.Controller lastController = null;

        Static fpsDisplay;
        Static crapDisplay;
        Static clockDisplay;

        TextInput debugConsole;

        Takai.Graphics.BitmapFont tinyFont;

        EffectsClass testEffect;

        Static renderSettingsConsole;
        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(MapInstance.RenderSettings);
                foreach (var child in renderSettingsConsole.Children)
                    ((CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);

                AddChild(renderSettingsConsole);
            }
        }

        public Game(MapInstance map)
        {
            GameInstance.Current = new GameInstance();

            Map = map ?? throw new ArgumentNullException("There must be a map to play");

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            map.renderSettings.drawBordersAroundNonDrawingEntities = true;

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
            AddChild(clockDisplay = new Static()
            {
                Position = new Vector2(0, 20),
                VerticalAlignment = Alignment.Start,
                HorizontalAlignment = Alignment.Middle,
                Color = new Color(1, 1, 1, 0.5f),
            });

            debugConsole = new TextInput()
            {
                Position = new Vector2(20),
                Size = new Vector2(400, 30),
                VerticalAlignment = Alignment.End,
                Font = Takai.Data.Cache.Load<Takai.Graphics.BitmapFont>("UI/Fonts/xbox.bfnt"),
            };
            debugConsole.Submit += delegate (object sender, EventArgs e)
            {
                var inp = (TextInput)sender;
                ParseCommand(inp.Text);
                inp.RemoveFromParent();
                inp.Text = String.Empty;
            };

            tinyFont = Takai.Data.Cache.Load<Takai.Graphics.BitmapFont>("UI/Fonts/UITiny.bfnt");

            testEffect = Takai.Data.Cache.Load<EffectsClass>("Effects/Damage.fx.tk");
        }

        protected override void OnMapChanged(EventArgs e)
        {
            GameInstance.Current.ElapsedRealTime = TimeSpan.Zero;

            Map.updateSettings = MapInstance.UpdateSettings.Game;
            Map.renderSettings = MapInstance.RenderSettings.Default;

            Map.CleanupAll(
                MapInstance.CleanupOptions.DeadEntities |
                MapInstance.CleanupOptions.Particles
            );

            var players = new System.Collections.Generic.List<Entities.ActorInstance>();
            var enemies = new System.Collections.Generic.List<Entities.ActorInstance>();

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
                GameInstance.Current.players = players.GetRange(0, numPlayers);
                for (int i = numPlayers; i < players.Count; ++i)
                    Map.Destroy(players[i]);

                player = GameInstance.Current.players[0];
            }

            Map.ActiveCamera = new Camera(player); //todo: resume control
            x = false;
        }

        string GetClockText(TimeSpan time)
        {
            return $"{(int)time.TotalHours:D2}:"
                    + $"{time.Minutes:D2}:"
                    + $"{time.Seconds:D2}."
                    + $"{time.Milliseconds:D3}";
        }

        bool x = false;
        protected override void UpdateSelf(GameTime time)
        {
            GameInstance.Current.ElapsedRealTime += time.ElapsedGameTime;
            clockDisplay.Text = $"{GetClockText(GameInstance.Current.ElapsedRealTime)}\n{GetClockText(Map.ElapsedTime)}";
            clockDisplay.AutoSize();

            if(!x)
            {
                x = true;
                Takai.Data.Cache.CleanupStaleReferences(); //todo: find better place for this (editor needs to be fully out of scope)
            }

            for (int i = 0; i < (GameInstance.Current.players?.Count ?? 0); ++i)
            {
                //var region = new Rectangle(GameInstance.Current.players[i].Position.ToPoint(), new Point(1));
                var region = Map.ActiveCamera.VisibleRegion;
                region.Inflate(Map.Class.SectorPixelSize, Map.Class.SectorPixelSize);
                Map.BuildHeuristic((GameInstance.Current.players[i].Position / Map.Class.TileSize).ToPoint(), region, i > 0);
            }

            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";
            fpsDisplay.AutoSize();

            crapDisplay.Text = $"TimeScale:{Map.TimeScale}\nZoom:{Map.ActiveCamera.Scale}";
            crapDisplay.AutoSize();

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
                                    Takai.LogBuffer.Append("Removing all fluids");
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
                                var newEnts = new System.Collections.Generic.List<EntityInstance>();
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

                        System.Collections.Generic.Dictionary<string, object> type;
                        try
                        {
                            type = Takai.Data.Serializer.DescribeType(Takai.Data.Serializer.RegisteredTypes[words[1]]);
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

                case "exit":
                case "quit":
                    Takai.Runtime.IsExiting = true;
                    break;

                default:
                    Takai.LogBuffer.Append("Unkown command: " + words[0]);
                    break;
            }
        }
        Vector2 lastClick;

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

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Takai.Runtime.IsExiting = true;
                return false;
            }

            //possess actors
#if DEBUG
            if (InputState.IsMod(KeyMod.Alt) && InputState.IsPress(MouseButtons.Left))
            {
                var targets = Map.FindEntities(worldMousePos, 5);

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

            DefaultFont.Draw(spriteBatch, $"Total entities:{Map.AllEntities.Count()}\nTotal Particles:{particleCount}", new Vector2(20), Color.Orange);

            foreach (var ent in Map.EnumerateVisibleEntities())
            {
                if (!(ent is Entities.ActorInstance actor))
                    continue;

                var pos = Vector2.Transform(actor.Position, Map.ActiveCamera.Transform)
                            - new Vector2(ent.Radius * 1.5f * Map.ActiveCamera.Scale);

                DefaultFont.Draw(spriteBatch, $"{actor.CurrentHealth} {string.Join(",", actor.ActiveAnimations)}", pos, Color.Tomato);
                if (actor.Weapon is Weapons.GunInstance gun)
                {
                    pos.Y += 15;
                    DefaultFont.Draw(spriteBatch, $"{gun.AmmoCount} {gun.State} {gun.Charge:N2}", pos, Color.LightSteelBlue);
                }
            }
        }
    }
}

//todo: scale animation speed by charge/discharge time, make charge/discharge range