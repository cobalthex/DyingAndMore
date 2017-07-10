﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class TriggersEditorMode : EditorMode
    {
        bool isNewTrigger = false;
        Vector2 savedWorldPos;

        Takai.Game.Trigger activeTrigger = null;

        public TriggersEditorMode(Editor editor)
            : base("Triggers", editor)
        {
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;
        }

        public override void Start()
        {
            editor.Map.renderSettings |= Takai.Game.Map.RenderSettings.DrawTriggers;
        }

        public override void End()
        {
            editor.Map.renderSettings &= ~Takai.Game.Map.RenderSettings.DrawTriggers;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (!base.HandleInput(time))
                return false;

            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

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
                    activeTrigger = new Takai.Game.Trigger(new Rectangle((int)currentWorldPos.X, (int)currentWorldPos.Y, 1, 1));
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
                    activeTrigger.Region = new Rectangle(start, diff);
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

            return true;
        }

        void SelectTrigger(Vector2 Position)
        {
            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
            var sectorIndex = editor.Map.GetOverlappingSector(currentWorldPos);
            var sector = editor.Map.Sectors[sectorIndex.Y, sectorIndex.X];

            for (int i = sector.triggers.Count - 1; i >= 0; --i)
            {
                if (sector.triggers[i].Region.Contains(Position))
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
                editor.Map.DrawRect(activeTrigger.Region, Color.GreenYellow);
        }
    }
}