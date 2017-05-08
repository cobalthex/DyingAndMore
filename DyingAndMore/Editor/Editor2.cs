using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Data;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class Editor2 : Takai.UI.MapView
    {
        ModeSelector selector;
        Takai.UI.List renderSettingsConsole;

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
                checkbox.OnClick += delegate (Takai.UI.Static sender, Takai.UI.ClickEventArgs args)
                {
                    setting.SetValue(Map.renderSettings, ((Takai.UI.CheckBox)sender).IsChecked);
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

            return base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);


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
