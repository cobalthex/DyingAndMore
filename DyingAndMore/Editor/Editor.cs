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

        //public virtual void OpenConfigurator(bool DidClickOpen) { }

        public virtual void Start() { }
        public virtual void End() { }
    }

    class Editor : MapView
    {
        ModeSelector modes;
        List renderSettingsConsole;
        Static fpsDisplay;

        public Editor()
        {
            Map = Serializer.CastType<Takai.Game.Map>(Serializer.TextDeserialize("Data/Maps/maze2.map.tk"));
            Map.InitializeGraphics();
            Map.ActiveCamera = new EditorCamera();

            Map.updateSettings = Takai.Game.MapUpdateSettings.Editor;
            Map.renderSettings.drawBordersAroundNonDrawingEntities = true;
            Map.renderSettings.drawGrids = true;

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

            modes.AddMode(new TilesEditorMode(this)     { Font = smallFont });
            modes.AddMode(new DecalsEditorMode(this)    { Font = smallFont });
            modes.AddMode(new FluidsEditorMode(this)    { Font = smallFont });
            modes.AddMode(new EntitiesEditorMode(this)  { Font = smallFont });
            modes.AddMode(new GroupsEditorMode(this)    { Font = smallFont });
            modes.AddMode(new PathsEditorMode(this)     { Font = smallFont });
            modes.AddMode(new TriggersEditorMode(this)  { Font = smallFont });

            modes.ModeIndex = 0;

            #region render settings console

            renderSettingsConsole = new Takai.UI.List()
            {
                Name = "RenderSettings",
                Position = new Vector2(20, 0),
                HorizontalAlignment = Takai.UI.Alignment.Start,
                VerticalAlignment = Takai.UI.Alignment.Middle,
                Margin = 5
            };

            foreach (var setting in Map.renderSettings.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                var checkbox = new Takai.UI.CheckBox()
                {
                    Name = setting.Name,
                    Text = Static.BeautifyMemberName(setting.Name),
                    Font = largeFont,
                    IsChecked = (bool)setting.GetValue(Map.renderSettings)
                };
                checkbox.Click += delegate (object sender, ClickEventArgs args)
                {
                    setting.SetValue(Map.renderSettings, ((CheckBox)sender).IsChecked);
                };
                checkbox.AutoSize();

                renderSettingsConsole.AddChild(checkbox);
            }
            renderSettingsConsole.AutoSize();

            #endregion

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
            if (InputState.IsPress(Keys.F2))
            {
                ToggleRenderSettingsConsole();
                return false;
            }

            return base.HandleInput(time);
        }

        void ToggleRenderSettingsConsole()
        {
            if (!renderSettingsConsole.RemoveFromParent())
            {
                //refresh individual render settings
                var settings = typeof(Takai.Game.Map.MapRenderSettings);
                foreach (var child in renderSettingsConsole.Children)
                    ((Takai.UI.CheckBox)child).IsChecked = (bool)settings.GetField(child.Name).GetValue(Map.renderSettings);

                AddChild(renderSettingsConsole);
            }
        }
    }
}
