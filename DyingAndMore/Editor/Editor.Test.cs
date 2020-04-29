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

        TrailInstance trail;

        public TestEditorMode(Editor editor)
            : base("Test", editor)
        {
            trail = Takai.Data.Cache.Load<TrailClass>("Effects/Trails/mouse.trail.tk").Instantiate();
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
                direction = Vector2.Normalize(pos - start);
                var trace = editor.Map.TraceTiles(start, direction, 10000);
                end = start + trace * direction;

                tangent = editor.Map.GetTilesCollisionTangent(end, direction);
            }
            else
                active = false;

            trail.Advance(pos, Vector2.Normalize(InputState.MouseDelta()));
            editor.Map.Spawn(trail);

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