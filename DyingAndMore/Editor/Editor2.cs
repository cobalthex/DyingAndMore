using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Data;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class Editor2 : Takai.UI.MapView
    {
        ModeSelector selector;
        List renderSettingsConsole;
        Static modeContainer;
        Static fpsDisplay;

        public Editor2()
        {
            Map = Serializer.CastType<Takai.Game.Map>(Serializer.TextDeserialize("Data/Maps/maze2.map.tk"));
            Map.InitializeGraphics(Takai.Runtime.GameManager.GraphicsDevice);
            Map.ActiveCamera = new EditorCamera();

            Map.updateSettings = Takai.Game.MapUpdateSettings.Editor;
            Map.renderSettings.drawBordersAroundNonDrawingEntities = true;
            Map.renderSettings.drawGrids = true;

            var smallFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UISmall.bfnt");
            var largeFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/UILarge.bfnt");

            AddChild(selector = new ModeSelector(largeFont, smallFont)
            {
                HorizontalAlignment = Takai.UI.Alignment.Middle,
                VerticalAlignment = Takai.UI.Alignment.Start,
                Position = new Vector2(0, 40)
            });

            AddChild(fpsDisplay = new Static()
            {
                Position = new Vector2(20),
                VerticalAlignment = Alignment.End,
                HorizontalAlignment = Alignment.End,
                Font = smallFont
            });
            AddChild(new TrackBar()
            {
                Minimum = 0,
                Maximum = 100,
                Size = new Vector2(200, 30),
                Position = new Vector2(0, 100),
                HorizontalAlignment = Alignment.Middle
            });
            AddChild(new ScrollBar()
            {
                ContentPosition = 10,
                ContentSize = 2000,
                Position = new Vector2(100),
                Size = new Vector2(20, 100),
                VerticalAlignment = Alignment.Stretch
            });
            AddChild(new ScrollBar()
            {
                ContentPosition = 10,
                ContentSize = 200,
                Position = new Vector2(130, 100),
                Size = new Vector2(100, 20),
                Direction = Direction.Horizontal
            });

            AddChild(new Selectors.Selector2()
            {
                Size = new Vector2(300, 1),
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.End,
                ItemCount = 200,
                ItemSize = new Point(64)
            });

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
                    Text = BeautifyPropName(setting.Name),
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
        }

        protected override bool UpdateSelf(GameTime time)
        {
            if (InputState.IsPress(Keys.F2))
                ToggleRenderSettingsConsole();
            else if (InputState.IsPress(Keys.Q) && InputState.IsMod(KeyMod.Control))
                Takai.Runtime.GameManager.Exit();

            fpsDisplay.Text = "FPS: " + (1000 / time.ElapsedGameTime.TotalMilliseconds).ToString("N2");
            fpsDisplay.AutoSize();
            return base.UpdateSelf(time);
        }

        static string BeautifyPropName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            var builder = new System.Text.StringBuilder(name.Length + 4);
            builder.Append(char.ToUpper(name[0]));
            for (int i = 1; i < name.Length; ++i)
            {
                if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    builder.Append(' ');
                    builder.Append(char.ToLower(name[i]));
                }
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
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
