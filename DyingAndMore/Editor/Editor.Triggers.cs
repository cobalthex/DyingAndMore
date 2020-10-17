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

        Takai.Game.Trigger activeTrigger = null;

        Static triggerSettings;

        public TriggersEditorMode(Editor editor)
            : base("Triggers", editor)
        {
            triggerSettings = Takai.Data.Cache.Load<Static>("UI/Editor/Trigger.ui.tk");
        }

        public override void Start()
        {
            editor.Map.renderSettings.drawTriggers = true;
        }

        public override void End()
        {
            //editor.Map.renderSettings.drawTriggers = false;
        }

        protected override bool HandleInput(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (activeTrigger != null && InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                var ui = triggerSettings.CloneHierarchy();
                ui.BindTo(activeTrigger);
                AddChild(ui);
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
                    activeTrigger = new Takai.Game.Trigger
                    {
                        Region = new Rectangle((int)currentWorldPos.X, (int)currentWorldPos.Y, 1, 1)
                    };
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
                    activeTrigger.Region = new Rectangle(start.X, start.Y, diff.X, diff.Y);
                }
            }

            else if (InputState.IsClick(MouseButtons.Left))
            {
                if (isNewTrigger)
                {
                    if (activeTrigger != null)
                    {
                        if (activeTrigger.Region.Width >= 10 && activeTrigger.Region.Height >= 10)
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
                if (sector.triggers[i].Region.Contains(Position.ToPoint()))
                {
                    activeTrigger = sector.triggers[i];
                    return;
                }
            }

            activeTrigger = null;
        }

        protected override void DrawSelf(DrawContext context)
        {
            if (activeTrigger != null)
            {
                editor.Map.DrawRect(activeTrigger.Region, Color.GreenYellow);
                var drawText = new Takai.Graphics.DrawTextOptions(
                    activeTrigger.Name,
                    Font,
                    TextStyle,
                    Color.White,
                    new Vector2(activeTrigger.Region.X + 10, activeTrigger.Region.Y + 5)
                );
                editor.MapTextRenderer.Draw(drawText);
            }
            base.DrawSelf(context);
        }
    }
}