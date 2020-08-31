using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    /// <summary>
    /// For debug purposes, playing around
    /// </summary>
    class TestEditorMode : EditorMode
    {
        TrailInstance trail;

        public TestEditorMode(Editor editor)
            : base("Test", editor)
        {
            trail = Takai.Data.Cache.Load<TrailClass>("Effects/Trails/mouse.trail.tk").Instantiate();
        }


        protected override bool HandleInput(GameTime time)
        {
            var pos = editor.Camera.ScreenToWorld(InputState.MouseVector);
            trail.Advance(pos, Vector2.Normalize(InputState.MouseDelta()));
            editor.Map.Spawn(trail);

            return base.HandleInput(time);
        }
    }
}