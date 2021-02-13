using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.UI;
using Takai.Data;
using Takai.Input;
using Takai.Game;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    public struct EditorConfiguration
    {
#pragma warning disable 0649
        public int maxMapSize;
        public float snapAngle;
        public MapBaseInstance.RenderSettings renderSettings;
#pragma warning restore 0649
    }

    public struct NamedPath
    {
        public string name;
        public VectorCurve path;
    }

    public class Editor : Static
    {
        public static Editor Current { get; set; }

        public Takai.Graphics.TextRenderer MapTextRenderer { get; private set; } =
            new Takai.Graphics.TextRenderer(Takai.Graphics.TextRenderer.Default);

        public MapInstance Map
        {
            get => _map;
            set
            {
                if (value == null)
                    throw new System.ArgumentNullException("Map cannot be null");
                if (_map == value)
                    return;

                bool hadMap = _map != null;
                _map = value;
                if (hadMap)
                    OnMapChanged();
            }
        }
        private MapInstance _map;

        public Camera Camera { get; set; } = new Camera
        {
            MoveSpeed = 2000
        };

        TabPanel modes;
        Static renderSettingsConsole;
        Static resizeDialog;
        Static playButton;
        Static resetZoom;

        UI.Balloon errorBalloon;

        bool isZoomSizing;

        Vector2 savedWorldPos, currentWorldPos;

        public EditorConfiguration config;

        public System.Collections.Generic.List<NamedPath> Paths { get; set; } = new System.Collections.Generic.List<NamedPath>();

        public Editor(MapInstance map)
        {
            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            var swatch = System.Diagnostics.Stopwatch.StartNew();
            config = Cache.Load<EditorConfiguration>("Editor.conf.tk", "Config");

            AddChild(modes = new TabPanel()
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch,
                Style = "Editor.ModeSelector",
                NavigateWithNumKeys = true
            });

            AddChild(playButton = new Static
            {
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.Middle,
                Text = "> PLAY >",
                Style = "Editor.Play",
            });

            errorBalloon = new UI.Balloon
            {
                HorizontalAlignment = Alignment.Middle,
                VerticalAlignment = Alignment.Middle,
                BackgroundColor = Color.Red,
                BorderColor = Color.DarkRed,
                Padding = new Vector2(30, 20)
            };

            resetZoom = new Graphic
            {
                Style = "Input",
                Sprite = Cache.Load<Texture2D>("UI/Editor/ResetZoom.png"),
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                IsEnabled = false
            };
            resetZoom.EventCommands[ClickEvent] = "ZoomWholeMap";
            AddChild(resetZoom);

            resizeDialog = Cache.Load<Static>("UI/Editor/ResizeMap.ui.tk");

            Map = map ?? throw new System.ArgumentNullException("There must be a map to edit");
            AddModes();

            AddChild(renderSettingsConsole = GeneratePropSheet(Map.renderSettings));
            renderSettingsConsole.Style = "Frame";
            renderSettingsConsole.IsEnabled = false;
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Alignment.Middle;

            swatch.Stop();
            Takai.LogBuffer.Append($"Loaded editor and map \"{Map.Class.Name}\" ({Map.Class.File}) in {swatch.ElapsedMilliseconds}msec");
            //todo: move this to mapChanged

            //todo: map zoom/something fucked if window not focused when loading

            On(ClickEvent, OnClick);
            On(DragEvent, OnDrag);
            
            CommandActions["ZoomWholeMap"] = delegate (Static sender, object arg)
            {
                ((Editor)sender).ZoomWholeMap();
            };

            Binding.Globals["Editor.Paths"] = Paths;

            Camera.MoveTo(new Vector2(map.Class.Bounds.Width / 2, map.Class.Bounds.Height / 2));
            Camera.ActualScale = 0;
            ZoomWholeMap();
        }

        void AddModes()
        {
            modes.AddChild(new TilesEditorMode(this));
            modes.AddChild(new DecalsEditorMode(this));
            modes.AddChild(new FluidsEditorMode(this));
            modes.AddChild(new EntitiesEditorMode(this));
            modes.AddChild(new SquadsEditorMode(this));
            modes.AddChild(new PathsEditorMode(this));
            modes.AddChild(new TriggersEditorMode(this));
            modes.AddChild(new TestEditorMode(this));
            modes.TabIndex = 0;
        }

        protected override void OnParentChanged(Static oldParent)
        {
            if (Parent == null)
                return;

            Map.updateSettings.SetEditor();
            Map.renderSettings = config.renderSettings.Clone();
            renderSettingsConsole?.BindTo(Map.renderSettings);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            base.ArrangeOverride(availableSize);
            Map.Class.OnViewportResized();
            Camera.Viewport = VisibleContentArea;
        }

        protected UIEventResult OnClick(Static sender, UIEventArgs e)
        {
            if (e.Source == playButton)
            {
                ((Editor)sender).SwitchToGame();
                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            if (dea.device == DeviceType.Mouse && dea.button == (int)MouseButtons.Middle)
            {
                //Camera.MoveTo(Camera.Position - Camera.LocalToWorld(dea.delta));
                Camera.Position -= Camera.LocalToWorld(dea.delta);

                resetZoom.IsEnabled = true;
                return UIEventResult.Handled;
            }

            if (dea.device == DeviceType.Touch)
            {
                if (dea.button == 2) //three finger pan
                    Camera.Position -= Camera.LocalToWorld(dea.delta);
                else if (dea.button == 1) //two finger pan+scale
                {
                    Camera.Position -= Camera.LocalToWorld(dea.delta * Camera.ActualScale);
                    var zoomDelta = InputState.TouchPinchDelta();
                    var diagonal = ContentArea.Size.ToVector2().Length() * 2;
                    Camera.Scale += (Camera.Scale * zoomDelta) / diagonal;
                }
                
                resetZoom.IsEnabled = true;
                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected void OnMapChanged()
        {
            foreach (var mode in modes.Children)
            {
                if (mode is EditorMode em)
                    em.OnMapChanged();
            }

            renderSettingsConsole?.BindTo(Map.renderSettings);
            
            Camera.MoveTo(new Vector2(Map.Class.Bounds.Width / 2, Map.Class.Bounds.Height / 2));
            Camera.ActualScale = 0;
            ZoomWholeMap();
        }

        public void ZoomWholeMap()
        {
            var mapSize = new Vector2(Map.Class.Width, Map.Class.Height) * Map.Class.TileSize;
            var xyScale = new Vector2(Takai.Runtime.GraphicsDevice.Viewport.Width - 20,
                                      Takai.Runtime.GraphicsDevice.Viewport.Height - 20) / mapSize;

            if (float.IsNaN(Camera.ActualPosition.X) || float.IsNaN(Camera.ActualPosition.Y))
                Camera.MoveTo(new Vector2(Map.Class.Bounds.Width / 2, Map.Class.Bounds.Height / 2));
            if (float.IsNaN(Camera.ActualScale))
                Camera.ActualScale = 0;

            Camera.Scale = MathHelper.Clamp(System.Math.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            Camera.Position = mapSize / 2;

            if (resetZoom != null)
                resetZoom.IsEnabled = false;
        }

        void SwitchToGame()
        {
            bool foundPlayer = false;
            foreach (var ent in Map.AllEntities)
            {
                if (ent is Game.Entities.ActorInstance actor && actor.Controller is Game.Entities.InputController)
                {
                    foundPlayer = true;
                    break;
                }
            }

            //allow overriding?
            if (!foundPlayer)
            {
                errorBalloon.ResetTimer();
                errorBalloon.Text = "There must be at least one player\n(controller = InputController) to test the map";
                AddChild(errorBalloon);
                return;
            }

            ((EditorMode)(modes.ActiveTab))?.End();

            var tmpFile = Path.GetTempFileName();
            Map.Save(tmpFile);

            if (Game.GameInstance.Current == null)
                Game.GameInstance.Current = new Game.GameInstance(new Game.Game { Map = Map });
            else
                Game.GameInstance.Current.Map = Map;

            Parent.ReplaceAllChildren(Game.GameInstance.Current);
        }

        void UpdateCamera(GameTime time)
        {
            var worldMousePos = Camera.ScreenToWorld(InputState.MouseVector);

            var d = Vector2.Zero;
            if (InputState.IsButtonDown(Keys.A) || InputState.IsButtonDown(Keys.Left))
                d -= Vector2.UnitX;
            if (InputState.IsButtonDown(Keys.W) || InputState.IsButtonDown(Keys.Up))
                d -= Vector2.UnitY;
            if (InputState.IsButtonDown(Keys.D) || InputState.IsButtonDown(Keys.Right))
                d += Vector2.UnitX;
            if (InputState.IsButtonDown(Keys.S) || InputState.IsButtonDown(Keys.Down))
                d += Vector2.UnitY;

            if (d != Vector2.Zero)
            {
                d.Normalize();
                d = d * Camera.MoveSpeed * (float)time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                Camera.Position += Camera.LocalToWorld(d);
                resetZoom.IsEnabled = true;
            }

            if (InputState.HasScrolled()) //special event?
            {
                var delta = (Camera.Scale * InputState.ScrollDelta()) / 1024f;
                if (InputState.IsButtonDown(Keys.LeftShift))
                    Camera.Rotation += delta;
                else
                {
                    //todo: this is a little wobbly
                    Camera.Position += (worldMousePos - Camera.Position) * (delta / Camera.ActualScale);
                    Camera.Scale += delta;
                    if (System.Math.Abs(Camera.Scale - 1) < 0.1f) //snap to 100% when near
                        Camera.Scale = 1;
                }
                resetZoom.IsEnabled = true;
            }
        }

        protected override void UpdateSelf(GameTime time)
        {
            //fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";

            Map.BeginUpdate();
            Map.MarkRegionActive(Camera);
            Map.Update(time);

            if (isZoomSizing)
            {
                var dist = Vector2.Distance(savedWorldPos, currentWorldPos);
                var whRatio = new Vector2(Camera.Viewport.Width, Camera.Viewport.Height);
                whRatio.Normalize();
                if (!float.IsNaN(whRatio.X))
                {
                    whRatio *= Takai.Util.Sign(currentWorldPos - savedWorldPos);

                    //todo: allow breaking into quadrants

                    Map.DrawRect(Takai.Util.AbsRectangle(savedWorldPos, savedWorldPos + dist * whRatio), Color.Aquamarine);
                }
            }

            Camera.Scale = MathHelper.Clamp(Camera.Scale, 0.1f, 10f); //todo: make ranges global and move to some game settings
            //check if scale changed and toggle reset button?
            Camera.Update(time);

            base.UpdateSelf(time);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.F1) ||
                InputState.IsAnyPress(Buttons.Start))
            {
                SwitchToGame();
                return false;
            }

#if DEBUG
            if (InputState.IsPress(Keys.F2))
            {
                renderSettingsConsole.IsEnabled ^= true;
                return false;
            }
#endif

            //if (InputState.Gestures.TryGetValue(GestureType.Pinch, out var gesture))
            //{
            //    if (Vector2.Dot(gesture.Delta, gesture.Delta2) > 0)
            //    {
            //        //todo: maybe add velocity
            //        Camera.Position -= Vector2.TransformNormal(gesture.Delta2, Matrix.Invert(Camera.Transform)) / 2;
            //    }
            //    //scale
            //    else
            //    {
            //        var lp1 = gesture.Position - gesture.Delta;
            //        var lp2 = gesture.Position2 - gesture.Delta2;
            //        var dist = Vector2.Distance(gesture.Position, gesture.Position2);
            //        var ld = Vector2.Distance(lp1, lp2);

            //        var scale = (dist / ld) / 100;
            //        Camera.Scale += scale;
            //    }
            //}

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

            //todo: switch editor to use input map6

            //Zoom to rect
            if (InputState.IsPress(Keys.Z))
            {
                isZoomSizing = true;

                if (float.IsNaN(currentWorldPos.X) || float.IsNaN(currentWorldPos.Y))
                    ZoomWholeMap();
                savedWorldPos = currentWorldPos = Camera.ScreenToWorld(InputState.MouseVector);
                return false;
            }
            if (InputState.IsClick(Keys.Z))
            {
                isZoomSizing = false;

                var dist = Vector2.Distance(savedWorldPos, currentWorldPos);
                var whRatio = new Vector2(Camera.Viewport.Width, Camera.Viewport.Height);
                whRatio.Normalize();

                var sign = Takai.Util.Sign(currentWorldPos - savedWorldPos);
                whRatio *= sign;

                if (dist > 1)
                {
                    Vector2 a = savedWorldPos, b = (savedWorldPos + dist * whRatio);
                    Camera.Scale = Camera.Viewport.Width / (sign.X * (b - a).X);
                    Camera.Position = (a + b) / 2;
                }
                else
                    ZoomWholeMap();

                return false;
            }
            if (InputState.IsButtonDown(Keys.Z))
            {
                currentWorldPos = Camera.ScreenToWorld(InputState.MouseVector);
                return false;
            }

            if (InputState.IsMod(KeyMod.Control))
            {
                if (InputState.IsPress(Keys.S))
                {
                    using (var sfd = new System.Windows.Forms.SaveFileDialog()
                    {
                        Filter = "Dying and More! Maps (*.map.tk)|*.map.tk",
                        InitialDirectory = Path.GetDirectoryName(Map.Class.File),
                        RestoreDirectory = true,
                        SupportMultiDottedExtensions = true,
                        FileName = Path.GetFileName(Map.Class.File),
                    })
                    {
                        sfd.CustomPlaces.Add(Path.GetFullPath("Content/Mapsrc"));
                        sfd.CustomPlaces.Add(Path.GetFullPath("Maps"));
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            try
                            {
                                Map.Save(sfd.FileName);
                                AddChild(new UI.Balloon("Saved map to " + sfd.FileName));
                            }
                            catch
                            {
                                //todo
                            }
                        }
                    }
                    return false;
                }

                if (InputState.IsPress(Keys.O))
                {
                    using (var ofd = new System.Windows.Forms.OpenFileDialog()
                    {
                        Filter = "Dying and More! Maps (*.map.tk)|*.map.tk|Dying and More! Saves (*.d2sav)|*.d2sav",
                        InitialDirectory = Path.GetFullPath(Path.Combine(Cache.ContentRoot, "Mapsrc")),
                        RestoreDirectory = true,
                        SupportMultiDottedExtensions = true,
                    })
                    {
                        ofd.CustomPlaces.Add(Path.GetFullPath("Content/Mapsrc")); //test if exists in release mode?
                        ofd.CustomPlaces.Add(Path.GetFullPath("Maps"));
                        ofd.CustomPlaces.Add(Path.GetFullPath("Saves"));
                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            //try
                            {
                                if (ofd.FileName.EndsWith(".d2sav"))
                                {
                                    var instance = Cache.Load<MapInstance>(ofd.FileName);
                                    if (instance.Class != null)
                                    {
                                        instance.Class.InitializeGraphics();
                                        Parent.ReplaceAllChildren(new Editor(instance));
                                    }
                                }
                                else
                                {
                                    var mapFile = Cache.Load(ofd.FileName);
                                    if (mapFile is MapClass mapClass)
                                    {
                                        mapClass.InitializeGraphics();
                                        Parent.ReplaceAllChildren(new Editor((MapInstance)mapClass.Instantiate()));
                                    }
                                    else if (mapFile is MapInstance mapInst)
                                    {
                                        if (mapInst.Class == null)
                                            throw new System.NullReferenceException("Map class cannot be null");
                                        mapInst.Class.InitializeGraphics();
                                        Parent.ReplaceAllChildren(new Editor(mapInst));
                                    }
                                    else
                                        throw new System.NotSupportedException("Unknown map format");
                                }

                                Cache.CleanupStaleReferences();
                            }
                            //catch
                            //{
                            //    //todo
                            //}
                        }
                    }
                    return false;
                } 

                if (InputState.IsPress(Keys.N))
                {
                    var newMap = Cache.Load<Static>("UI/Editor/NewMap.ui.tk");
                    newMap.CommandActions["Create"] = delegate (Static sender, object arg)
                    {
                        newMap.RemoveFromParent();

                        var name = sender.FindChildByName("name").Text;
                        var width = sender.FindChildByName<NumericBase>("width").Value;
                        var height = sender.FindChildByName<NumericBase>("height").Value;
                        var tileset = Cache.Load<Tileset>(sender.FindChildByName<FileInputBase>("tileset").Value);

                        MaterialInteractions mtls = null;
                        try
                        {
                            mtls = Cache.Load<MaterialInteractions>("Materials/Default.mtl.tk");
                        }
                        catch { }

                        var map = new MapClass
                        {
                            Name = name,
                            Tiles = new short[height, width],
                            Tileset = tileset,
                            MaterialInteractions = mtls
                        };
                        map.InitializeGraphics();
                        Map = (MapInstance)map.Instantiate();
                    };

                    AddChild(newMap);
                }

                if (InputState.IsPress(Keys.R))
                {
                    resizeDialog.BindTo(Map);
                    //todo: listen to commands
                    AddChild(resizeDialog);
                }
            }

#endif

            UpdateCamera(time);

            return base.HandleInput(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);
            Map.Draw(Camera);

            //ignore camera scale?
            var gd = context.spriteBatch.GraphicsDevice;
            MapTextRenderer.Present(
                gd,
                Camera.Transform * Matrix.CreateOrthographicOffCenter(gd.Viewport.Bounds, -1, 1)
            );
        }
    }
}
