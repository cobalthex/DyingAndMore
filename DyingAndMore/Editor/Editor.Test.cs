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

            On(DragEvent, OnDrag);
        }

        UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            var pos = editor.Camera.ScreenToWorld(LocalToScreen(dea.position));
            trail.Advance(pos, Vector2.Normalize(dea.delta));
            editor.Map.Spawn(trail);

            return UIEventResult.Handled;
        }
    }
}