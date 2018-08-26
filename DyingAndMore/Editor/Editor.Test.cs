using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;
using Takai.Input;

namespace DyingAndMore.Editor
{
    /// <summary>
    /// For debug purposes, playing around
    /// </summary>
    class TestEditorMode : EditorMode
    {
        Vector2 start, end, direction, tangent;
        bool active = false;

        public TestEditorMode(Editor editor)
            : base("Test", editor)
        {
            VerticalAlignment = Takai.Data.Alignment.Stretch;
            HorizontalAlignment = Takai.Data.Alignment.Stretch;

        }

        public override void Start()
        {
        }

        public override void End()
        {
        }

        protected override bool HandleInput(GameTime time)
        {
            var pos = editor.Camera.ScreenToWorld(InputState.MouseVector);
            if (InputState.IsPress(MouseButtons.Left))
            {
                start = pos;
                active = true;
            }
            if (InputState.IsButtonDown(MouseButtons.Left))
            {
                var trace = editor.Map.TraceTiles(start, pos);
                direction = Vector2.Normalize(pos - start);
                end = trace;

                tangent = editor.Map.GetTilesCollisionTangent(end, direction);
            }
            else
                active = false;

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (active)
            {
                editor.Map.DrawLine(start, end, Color.PeachPuff);
                editor.Map.DrawLine(end - tangent * 10, end + tangent * 10, Color.IndianRed);
            }

            base.DrawSelf(spriteBatch);
        }
    }
}