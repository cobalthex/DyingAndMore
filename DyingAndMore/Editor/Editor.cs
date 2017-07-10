﻿using Microsoft.Xna.Framework;
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

        //public virtual void OpenConfigurator(bool DidClickOpen) { }

        public virtual void Start() { }
        public virtual void End() { }
    }

    class Editor : MapView
    {
        ModeSelector modes;
        Static renderSettingsConsole;
        Static fpsDisplay;
        Static resizeDialog;

        public Editor(Takai.Game.Map map)
        {
            Map = map ?? throw new System.ArgumentNullException("There must be a map to edit");

            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            var smallFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UISmall.bfnt");
            var largeFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UILarge.bfnt");

            AddChild(modes = new ModeSelector(largeFont, smallFont)
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            });

            AddChild(fpsDisplay = new Static()
            {
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
                Font = smallFont
            });

            modes.AddMode(new TilesEditorMode(this));
            modes.AddMode(new DecalsEditorMode(this));
            modes.AddMode(new FluidsEditorMode(this));
            modes.AddMode(new EntitiesEditorMode(this));
            //modes.AddMode(new GroupsEditorMode(this));
            modes.AddMode(new PathsEditorMode(this));
            modes.AddMode(new TriggersEditorMode(this));

            modes.ModeIndex = 0;

            //renderSettingsConsole = GeneratePropSheet(map.renderSettings, DefaultFont, DefaultColor);
            renderSettingsConsole = new Static();
            renderSettingsConsole.Position = new Vector2(100, 0);
            renderSettingsConsole.VerticalAlignment = Alignment.Middle;

            resizeDialog = Serializer.TextDeserialize<Static>("Defs/UI/Editor/ResizeMap.ui.tk");
        }

        protected override void OnMapChanged(System.EventArgs e)
        {
            Map.ActiveCamera = new EditorCamera();

            Map.updateSettings = Takai.Game.MapUpdateSettings.Editor;
            Map.renderSettings |= Takai.Game.Map.RenderSettings.DrawBordersAroundNonDrawingEntities;
            Map.renderSettings |= Takai.Game.Map.RenderSettings.DrawGrids;
            Map.renderSettings |= Takai.Game.Map.RenderSettings.DrawSectorsOnGrid;

            //start zoomed out to see the whole map
            var mapSize = new Vector2(Map.Width, Map.Height) * Map.TileSize;
            var xyScale = new Vector2(Takai.Runtime.GraphicsDevice.Viewport.Width - 20,
                                      Takai.Runtime.GraphicsDevice.Viewport.Height - 20) / mapSize;
            Map.ActiveCamera.Scale = MathHelper.Clamp(MathHelper.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            Map.ActiveCamera.MoveTo(mapSize / 2);
        }

        protected override void UpdateSelf(GameTime time)
        {
            fpsDisplay.Text = "FPS: " + (1000 / time.ElapsedGameTime.TotalMilliseconds).ToString("N2");
            fpsDisplay.AutoSize();

            base.UpdateSelf(time);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.F1))
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

            if (InputState.IsPress(Keys.F5))
            {
                //todo: reload all defs
                return false;
            }

            if (InputState.IsMod(KeyMod.Control))
            {
                if (InputState.IsPress(Keys.S))
                {
                    using (var sfd = new System.Windows.Forms.SaveFileDialog()
                    {
                        Filter = "Dying and More! Maps (*.map.tk)|*.map.tk",
                        RestoreDirectory = true
                    })
                    {
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            try
                            {
                                Serializer.TextSerialize(sfd.FileName, Map);
                            }
                            catch
                            {
                                //todo
                            }
                        }
                    }
                    return false;
                }

                if (InputState.IsPress(Keys.R))
                {
                    var widthInput = (NumericInput)resizeDialog.FindChildByName("width", false);
                    var heightInput  = (NumericInput)resizeDialog.FindChildByName("height", false);

                    widthInput.Value = Map.Width;
                    heightInput.Value = Map.Height;

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

            return base.HandleInput(time);
        }

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
    }
}
