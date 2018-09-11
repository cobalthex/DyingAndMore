using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class TriggersEditorMode : EditorMode
    {
        bool isNewTrigger = false;
        Vector2 savedWorldPos;

        Takai.Game.TriggerInstance activeTrigger = null;

        Static triggerSettings;
        TextInput triggerSettingsNameInput;

        public TriggersEditorMode(Editor editor)
            : base("Triggers", editor)
        {
            VerticalAlignment = Alignment.Stretch;
            HorizontalAlignment = Alignment.Stretch;

            triggerSettingsNameInput = new TextInput();
            triggerSettingsNameInput.TextChanged += ActiveTriggerTextChanged;
            triggerSettingsNameInput.SizeToContain();

            var closeButton = new Static
            {
                Text = "Close",
                HorizontalAlignment = Alignment.Stretch,
                BorderColor = Color.White,
                Padding = new Vector2(10)
            };
            closeButton.SizeToContain();
            closeButton.Click += delegate { triggerSettings.RemoveFromParent(); };

            var label = new Static { Text = "Name" };
            label.SizeToContain();

            triggerSettings = new List(
                label,
                triggerSettingsNameInput,
                closeButton
            )
            {
                BackgroundColor = new Color(20, 20, 20, 255),
                HorizontalAlignment = Alignment.Middle,
                VerticalAlignment = Alignment.Middle,
                Margin = 10
            };
            triggerSettings.SizeToContain();
        }

        public override void Start()
        {
            editor.Map.renderSettings.drawTriggers = true;
        }

        public override void End()
        {
            editor.Map.renderSettings.drawTriggers = false;
        }

        void ActiveTriggerTextChanged(object sender, System.EventArgs e)
        {
            if (activeTrigger?.Class == null)
                return;

            activeTrigger.Class.Name = ((TextInput)sender).Text;
        }

        protected override bool HandleInput(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (activeTrigger != null && InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                triggerSettingsNameInput.Text = activeTrigger.Class.Name;
                triggerSettingsNameInput.HasFocus = true;
                AddChild(triggerSettings);
                return false;
            }

            if (InputState.IsPress(MouseButtons.Left))
            {
                SelectTrigger(currentWorldPos);
                savedWorldPos = currentWorldPos;
            }

            else if (InputState.IsButtonDown(MouseButtons.Left) &&
                    (isNewTrigger || InputState.HasMouseDragged(MouseButtons.Left)))
            {
                if (!isNewTrigger)
                {
                    activeTrigger = new Takai.Game.TriggerClass
                    {
                        Region = new Rectangle((int)currentWorldPos.X, (int)currentWorldPos.Y, 1, 1)
                    }.Instantiate();
                    isNewTrigger = true;
                }
                else if (activeTrigger != null)
                {
                    var start = savedWorldPos.ToPoint();
                    var end = currentWorldPos.ToPoint();

                    if (start.X > end.X)
                    {
                        var tmp = start.X;
                        start.X = end.X;
                        end.X = tmp;
                    }
                    if (start.Y > end.Y)
                    {
                        var tmp = start.Y;
                        start.Y = end.Y;
                        end.Y = tmp;
                    }

                    var diff = end - start;
                    activeTrigger.Class.Region = new Rectangle(start.X, start.Y, diff.X, diff.Y);
                }
            }

            else if (InputState.IsClick(MouseButtons.Left))
            {
                if (isNewTrigger)
                {
                    if (activeTrigger != null)
                    {
                        if (activeTrigger.Class.Region.Width >= 10 && activeTrigger.Class.Region.Height >= 10)
                            editor.Map.AddTrigger(activeTrigger);
                        else
                            activeTrigger = null;
                    }
                    isNewTrigger = false;
                }
            }

            if (InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                isNewTrigger = false;
                activeTrigger = null;
                savedWorldPos = currentWorldPos;
            }

            if (activeTrigger != null && InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                if (!isNewTrigger)
                    editor.Map.Destroy(activeTrigger);

                activeTrigger = null;
                isNewTrigger = false;
                savedWorldPos = currentWorldPos;
            }

            return base.HandleInput(time);
        }

        void SelectTrigger(Vector2 Position)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
            var sectorIndex = editor.Map.GetOverlappingSector(currentWorldPos);
            var sector = editor.Map.Sectors[sectorIndex.Y, sectorIndex.X];

            for (int i = sector.triggers.Count - 1; i >= 0; --i)
            {
                if (sector.triggers[i].Class.Region.Contains(Position.ToPoint()))
                {
                    activeTrigger = sector.triggers[i];
                    return;
                }
            }

            activeTrigger = null;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (activeTrigger != null)
            {
                editor.Map.DrawRect(activeTrigger.Class.Region, Color.GreenYellow);
                var textPos = new Vector2(activeTrigger.Class.Region.X + 5, activeTrigger.Class.Region.Y + 5);
                DefaultFont?.Draw(spriteBatch, activeTrigger.Class.Name, editor.Camera.WorldToScreen(textPos), Color.White);
            }
            base.DrawSelf(spriteBatch);
        }
    }
}