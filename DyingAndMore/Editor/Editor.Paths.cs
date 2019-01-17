using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class PathsEditorMode : EditorMode
    {
        Takai.Game.VectorCurve currentPath;

        public PathsEditorMode(Editor editor)
            : base("Paths", editor)
        {
        }

        public override void Start()
        {
        }

        public override void End()
        {
            currentPath = null;
        }

        void AddPathPoint(Vector2 position)
        {
            currentPath.AddPoint(position);
            Vector2 lp = Vector2.UnitX;

            for (int i = 0; i < currentPath.SectionLengths.Count; ++i)
            {
                var sl = (int)System.Math.Ceiling(currentPath.SectionLengths[i] / 20);
                var start = currentPath.Values[i];
                var delta = (currentPath.Values[i + 1].position - start.position) / sl;
                var last = start.value;

                for (int t = 1; t <= sl + 1; ++t)
                {
                    var next = currentPath.Evaluate(start.position + (t * delta));
                    var dir = Vector2.Normalize(next - last);
                    last = next;
                }
            }
        }

        protected override bool HandleInput(GameTime time)
        {
            if (currentPath != null)
            {
                if (Takai.Input.InputState.IsPress(Takai.Input.MouseButtons.Left))
                {
                    AddPathPoint(editor.Camera.ScreenToWorld(Takai.Input.InputState.MouseVector));
                    return false;
                }
            }
            else
            {
                if (Takai.Input.InputState.IsPress(Takai.Input.MouseButtons.Left))
                {
                    editor.Paths.Add(currentPath = new Takai.Game.VectorCurve());
                    currentPath.AddPoint(editor.Camera.ScreenToWorld(Takai.Input.InputState.MouseVector));
                }
            }

            return base.HandleInput(time);
        }

        void DrawPath(Takai.Game.VectorCurve path, Color color)
        {
            int np = path.Values.Count;
            if (np < 2)
                return;

            Vector2 last = Vector2.Zero;
            //loop through each section for better accuracy
            for (int i = 0; i < path.SectionLengths.Count; ++i)
            {
                var sl = (int)System.Math.Ceiling(path.SectionLengths[i] / 20);
                var start = path.Values[i];
                var delta = (path.Values[i + 1].position - start.position) / sl;
                last = start.value;

                //draw X
                var xcol = new Color(color.B, color.R, color.G);
                editor.Map.DrawLine(last - new Vector2(3), last + new Vector2(3), xcol);
                editor.Map.DrawLine(last + new Vector2(-3, 3), last + new Vector2(3, -3), xcol);

                for (int t = 0; t <= sl; ++t)
                {
                    var next = path.Evaluate(start.position + (t * delta));
                    editor.Map.DrawLine(last, next, color);
                    var p = Takai.Util.Ortho(Vector2.Normalize(next - last));
                    editor.Map.DrawLine(last - p * 5, last + p * 5, new Color(color, 0.75f));
                    last = next;
                }
            }

            editor.Map.DrawLine(last - new Vector2(3), last + new Vector2(3), color);
            editor.Map.DrawLine(last + new Vector2(-3, 3), last + new Vector2(3, -3), color);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            foreach (var path in editor.Paths)
                DrawPath(path, path == currentPath ? Color.Gold : Color.Cyan);

            base.DrawSelf(spriteBatch);
        }
    }
}