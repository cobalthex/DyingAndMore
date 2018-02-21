using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Data;
using Takai.Input;
using Takai.UI;

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

    struct EditorConfiguration
    {
#pragma warning disable 0649
        public int maxMapSize;
        public float snapAngle;
        public Takai.Game.MapInstance.RenderSettings renderSettings;
#pragma warning restore 0649
    }

    class Editor : MapView
    {
        ModeSelector modes;
        Static renderSettingsConsole;
        Static fpsDisplay;
        Static resizeDialog;

        public EditorConfiguration config;

        public Editor(Takai.Game.MapInstance map)
        {
            var swatch = System.Diagnostics.Stopwatch.StartNew();
            config = Cache.Load<EditorConfiguration>("Editor.conf.tk", "Config");

            Map = map ?? throw new System.ArgumentNullException("There must be a map to edit");

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            var smallFont = Cache.Load<Takai.Graphics.BitmapFont>("UI/Fonts/UISmall.bfnt");
            var largeFont = Cache.Load<Takai.Graphics.BitmapFont>("UI/Fonts/UILarge.bfnt");

            AddChild(modes = new ModeSelector(largeFont, smallFont)
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            });

            AddChild(fpsDisplay = new Static
            {
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
                Font = smallFont
            });

            AddModes();
            modes.ModeIndex = 0;

            renderSettingsConsole = GeneratePropSheet(map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Alignment.Middle;

            resizeDialog = Cache.Load<Static>("UI/Editor/ResizeMap.ui.tk");

            swatch.Stop();
            Takai.LogBuffer.Append($"Loaded editor and map \"{map.Class.Name}\" ({map.Class.File}) in {swatch.ElapsedMilliseconds}msec");

            var sel = new DropdownSelect<string>();
            sel.Items.Add("test 1");
            sel.Items.Add("test 2");
            sel.Items.Add("test 3");
            sel.Items.Add("test 4");
            sel.Position = new Vector2(200);
            sel.Size = new Vector2(100, 20);
            AddChild(sel);
        }

        void AddModes()
        {
            modes.AddMode(new TilesEditorMode(this));
            modes.AddMode(new DecalsEditorMode(this));
            modes.AddMode(new FluidsEditorMode(this));
            modes.AddMode(new EntitiesEditorMode(this));
            modes.AddMode(new PathsEditorMode(this));
            modes.AddMode(new TriggersEditorMode(this));
        }

        protected override void OnMapChanged(System.EventArgs e)
        {
            Map.ActiveCamera = new EditorCamera();

            Map.updateSettings = Takai.Game.MapInstance.UpdateSettings.Editor;
            Map.renderSettings = config.renderSettings;

            //start zoomed out to see the whole map
            var mapSize = new Vector2(Map.Class.Width, Map.Class.Height) * Map.Class.TileSize;
            var xyScale = new Vector2(Takai.Runtime.GraphicsDevice.Viewport.Width - 20,
                                      Takai.Runtime.GraphicsDevice.Viewport.Height - 20) / mapSize;
            Map.ActiveCamera.Scale = MathHelper.Clamp(MathHelper.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            Map.ActiveCamera.MoveTo(mapSize / 2);
        }

        protected override void UpdateSelf(GameTime time)
        {
            fpsDisplay.Text = $"FPS:{(1000 / time.ElapsedGameTime.TotalMilliseconds):N2}";
            fpsDisplay.AutoSize();

            base.UpdateSelf(time);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.F1) ||
                InputState.IsAnyPress(Buttons.Start))
            {
                modes.Mode?.End();

                Parent.ReplaceAllChildren(new Game.Game(Map));
                return false;
            }

            if (InputState.IsPress(Keys.F2))
            {
                ToggleRenderSettingsConsole();
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
                                    var instance = Cache.Load<Takai.Game.MapInstance>(ofd.FileName);
                                    if (instance.Class != null)
                                    {
                                        instance.Class.InitializeGraphics();
                                        Parent.ReplaceAllChildren(new Editor(instance));
                                    }
                                }
                                else
                                {
                                    var mapClass = Cache.Load<Takai.Game.MapClass>(ofd.FileName);
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
                        var width = ((NumericBase)resizeMap.FindChildByName("width")).Value;
                        var height = ((NumericBase)resizeMap.FindChildByName("height")).Value;
                        var tileset = Cache.Load<Takai.Game.Tileset>(resizeMap.FindChildByName("tileset").Text);

                        var map = new Takai.Game.MapClass
                        {
                            Name = resizeMap.FindChildByName("name").Text,
                            Tiles = new short[height, width],
                            TilesImage = tileset.texture,
                            TileSize = tileset.size,
                        };
                        map.BuildTileMask(map.TilesImage);
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

            return base.HandleInput(time);
        }

        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(Takai.Game.MapInstance.RenderSettings);
                settings.GetTypeInfo();
                foreach (var child in renderSettingsConsole.Children)
                    ((CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);

                AddChild(renderSettingsConsole);
            }
        }
    }
}
