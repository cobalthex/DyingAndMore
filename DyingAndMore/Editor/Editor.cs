using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Takai.UI;
using Takai.Data;
using Takai.Input;
using Takai.Game;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class EditorMode : Static
    {
        protected readonly Editor editor;

        public EditorMode(string name, Editor editor)
        {
            Name = name;
            this.editor = editor;
        }

        public virtual void Start() { }
        public virtual void End() { }
    }

    public struct EditorConfiguration
    {
#pragma warning disable 0649
        public int maxMapSize;
        public float snapAngle;
        public MapInstance.RenderSettings renderSettings;
#pragma warning restore 0649
    }

    public class Editor : Static
    {
        public static Editor Current { get; set; }

        public MapInstance Map
        {
            get => _map;
            set
            {
                if (value == null)
                    throw new System.ArgumentNullException("Map cannot be null");
                if (_map == value)
                    return;

                _map = value;
                OnMapChanged();
            }
        }
        private MapInstance _map;

        public Camera Camera { get; set; } = new Camera();

        ModeSelector modes;
        Static renderSettingsConsole;
        Static fpsDisplay;
        Static resizeDialog;
        Static playButton;

        bool isZoomSizing;

        Vector2 savedWorldPos, currentWorldPos;

        public EditorConfiguration config;

        public System.Collections.Generic.List<VectorCurve> Paths { get; set; } = new System.Collections.Generic.List<VectorCurve>();

        public Editor(MapInstance map)
        {
            var swatch = System.Diagnostics.Stopwatch.StartNew();
            config = Cache.Load<EditorConfiguration>("Editor.conf.tk", "Config");

            Map = map ?? throw new System.ArgumentNullException("There must be a map to edit");

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            var smallFont = Cache.Load<Takai.Graphics.BitmapFont>("Fonts/UISmall.bfnt");
            var largeFont = Cache.Load<Takai.Graphics.BitmapFont>("Fonts/UILarge.bfnt");

            AddChild(modes = new ModeSelector(largeFont, smallFont)
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            });
            AddModes();
            modes.ModeIndex = 0;

            AddChild(fpsDisplay = new Static
            {
                Name = "FPS",
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
                Font = smallFont
            });

            AddChild(playButton = new Static
            {
                Name = "Play button",
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.Middle,
                Font = smallFont,
                Text = "> PLAY >",
                Padding = new Vector2(20)
            });
            playButton.SizeToContain();
            playButton.Click += delegate
            {
                SwitchToGame();
            };

            renderSettingsConsole = GeneratePropSheet(map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Alignment.Middle;

            resizeDialog = Cache.Load<Static>("UI/Editor/ResizeMap.ui.tk");

            swatch.Stop();
            Takai.LogBuffer.Append($"Loaded editor and map \"{map.Class.Name}\" ({map.Class.File}) in {swatch.ElapsedMilliseconds}msec");
            //todo: move this to mapChanged
        }

        void AddModes()
        {
            modes.AddMode(new TilesEditorMode(this));
            //modes.AddMode(new DecalsEditorMode(this));
            //modes.AddMode(new FluidsEditorMode(this));
            //modes.AddMode(new EntitiesEditorMode(this));
            //modes.AddMode(new PathsEditorMode(this));
            //modes.AddMode(new TriggersEditorMode(this));
            //modes.AddMode(new TestEditorMode(this));
        }

        protected override void OnParentChanged(ParentChangedEventArgs e)
        {
            if (Parent == null)
                return;

            Map.updateSettings.SetEditor();
            Map.renderSettings = config.renderSettings.Clone();
        }

        protected void OnMapChanged()
        {
            if (renderSettingsConsole != null)
                renderSettingsConsole.UserData = Map.renderSettings;

            Camera.ActualScale = 0;
            ZoomWholeMap();
        }

        public void ZoomWholeMap()
        {
            var mapSize = new Vector2(Map.Class.Width, Map.Class.Height) * Map.Class.TileSize;
            var xyScale = new Vector2(Takai.Runtime.GraphicsDevice.Viewport.Width - 20,
                                      Takai.Runtime.GraphicsDevice.Viewport.Height - 20) / mapSize;

            Camera.Scale = MathHelper.Clamp(System.Math.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            Camera.Position = mapSize / 2;
        }

        void SwitchToGame()
        {
            modes.Mode?.End();

            if (Game.GameInstance.Current == null)
                Game.GameInstance.Current = new Game.GameInstance(new Game.Game { Map = Map });
            else
                Game.GameInstance.Current.Map = Map;

            Parent.ReplaceAllChildren(Game.GameInstance.Current);
        }

        void UpdateCamera(GameTime time)
        {
            var worldMousePos = Camera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsButtonDown(MouseButtons.Middle))
                Camera.MoveTo(Camera.Position - Camera.NormalToWorld(InputState.MouseDelta()));
            else
            {
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
                    Camera.Position += Camera.NormalToWorld(d);
                }
            }

            if (InputState.HasScrolled())
            {
                var delta = (Camera.Scale * InputState.ScrollDelta()) / 1024f;
                if (InputState.IsButtonDown(Keys.LeftShift))
                    Camera.Rotation += delta;
                else
                {
                    //todo: this is a little wobbly
                    Camera.Position += (worldMousePos - Camera.Position) * (1 / Camera.Scale * delta);
                    Camera.Scale += delta;
                    if (System.Math.Abs(Camera.Scale - 1) < 0.1f) //snap to 100% when near
                        Camera.Scale = 1;
                }
            }

            Camera.Scale = MathHelper.Clamp(Camera.Scale, 0.1f, 10f); //todo: make ranges global and move to some game settings
            Camera.Viewport = VisibleBounds;
            Camera.Update(time);
        }

        protected override void UpdateSelf(GameTime time)
        {
            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";
            fpsDisplay.SizeToContain();

            Map.BeginUpdate();
            Map.MarkRegionActive(Camera);
            Map.Update(time);


            if (isZoomSizing)
            {
                var dist = Vector2.Distance(savedWorldPos, currentWorldPos);
                var whRatio = new Vector2(Camera.Viewport.Width, Camera.Viewport.Height);
                whRatio.Normalize();

                whRatio *= Takai.Util.Sign(currentWorldPos - savedWorldPos);

                //todo: allow breaking into quadrants

                Map.DrawRect(Takai.Util.AbsRectangle(savedWorldPos, savedWorldPos + dist * whRatio), Color.Aquamarine);
            }

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

            if (InputState.IsPress(Keys.F2))
            {
                ToggleRenderSettingsConsole();
                return false;
            }

            if (InputState.Gestures.TryGetValue(GestureType.Pinch, out var gesture))
            {
                if (Vector2.Dot(gesture.Delta, gesture.Delta2) > 0)
                {
                    //todo: maybe add velocity
                    Camera.Position -= Vector2.TransformNormal(gesture.Delta2, Matrix.Invert(Camera.Transform)) / 2;
                }
                //scale
                else
                {
                    var lp1 = gesture.Position - gesture.Delta;
                    var lp2 = gesture.Position2 - gesture.Delta2;
                    var dist = Vector2.Distance(gesture.Position, gesture.Position2);
                    var ld = Vector2.Distance(lp1, lp2);

                    var scale = (dist / ld) / 100;
                    Camera.Scale += scale;
                }
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

            //todo: switch editor to use input map6

            //Zoom to rect
            if (InputState.IsPress(Keys.Z))
            {
                isZoomSizing = true;
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

                if (dist != 0)
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
                        RestoreDirectory = true,
                        SupportMultiDottedExtensions = true,
                        FileName = System.IO.Path.GetFileName(Map.Class.File)
                    })
                    {
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            try
                            {
                                Map.SaveAsMap(sfd.FileName);
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
                        FileName = "",
                        InitialDirectory = System.IO.Path.Combine(Cache.Root, "Maps"),
                        RestoreDirectory = true,
                        SupportMultiDottedExtensions = true,
                    })
                    {
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
                                    var mapClass = Cache.Load<MapClass>(ofd.FileName);
                                    mapClass.InitializeGraphics();
                                    Parent.ReplaceAllChildren(new Editor(mapClass.Instantiate()));
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
                    var resizeMap = Cache.Load<Static>("UI/Editor/NewMap.ui.tk");

                    resizeMap.FindChildByName("create").Click += delegate (object sender, ClickEventArgs e)
                    {
                        var name = resizeMap.FindChildByName("name").Text;
                        var width = resizeMap.FindChildByName<NumericBase>("width").Value;
                        var height = resizeMap.FindChildByName<NumericBase>("height").Value;
                        var tileset = Cache.Load<Tileset>(resizeMap.FindChildByName<FileInputBase>("tileset").Value);

                        var map = new MapClass
                        {
                            Name = name,
                            Tiles = new short[height, width],
                            TileSize = tileset.size,
                            TilesImage = tileset.texture,
                        };
                        map.InitializeGraphics();
                        Map = map.Instantiate();
                        resizeMap.RemoveFromParent();
                    };

                    AddChild(resizeMap);
                }

                if (InputState.IsPress(Keys.R))
                {
                    var widthInput = (NumericInput)resizeDialog.FindChildByName("width");
                    var heightInput = (NumericInput)resizeDialog.FindChildByName("height");

                    widthInput.Maximum = heightInput.Maximum = config.maxMapSize;

                    widthInput.Value = Map.Class.Width;
                    heightInput.Value = Map.Class.Height;

                    var resizeBtn = resizeDialog.FindChildByName("resize", false);
                    var cancelBtn = resizeDialog.FindChildByName("cancel", false);

                    resizeBtn.Click += delegate
                    {
                        Map.Resize((int)widthInput.Value, (int)heightInput.Value);
                        resizeDialog.RemoveFromParent();
                    };

                    cancelBtn.Click += delegate
                    {
                        resizeDialog.RemoveFromParent();
                    };

                    AddChild(resizeDialog);
                }
            }

#endif

            UpdateCamera(time);

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Map.Draw(Camera);
        }

        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(MapInstance.RenderSettings);
                settings.GetTypeInfo();
                foreach (var child in renderSettingsConsole.Children)
                {
                    if (child.IsEnabled)
                        ((CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);
                }

                AddChild(renderSettingsConsole);
            }
        }
    }
}
