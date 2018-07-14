using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class PathsEditorMode : EditorMode
    {
        public static List<Takai.Game.VectorCurve> paths;

        Takai.UI.TrackBar trackBar;

        Takai.Game.TrailInstance trail;

        public PathsEditorMode(Editor editor)
            : base("Paths", editor)
        {
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;

            AddChild(trackBar = new Takai.UI.TrackBar()
            {
                Position = new Vector2(0, 100),
                Size = new Vector2(200, 30),
                HorizontalAlignment = Takai.UI.Alignment.Middle,
                Minimum = 0,
                Maximum = 1000,
                Value = 500,
                Increment = 50,
            });

            paths = new List<Takai.Game.VectorCurve>
            {
                new Takai.Game.VectorCurve()
            };

            trail = new Takai.Game.TrailClass
            {
                Color = Color.Blue,
                Width = 10,
            }.Instantiate();
            //trail = Takai.Data.Cache.Load<Takai.Game.TrailClass>("Effects/Trails/road.trail.tk").Instantiate();
        }

        public override void Start()
        {
        }

        public override void End()
        {
        }

        void AddPathPoint(Vector2 position)
        {
            paths[0].AddPoint(position);
            trail.Clear();
            Vector2 lp = Vector2.UnitX;

            var path = paths[0];

            for (int i = 0; i < path.SectionLengths.Count; ++i)
            {
                var sl = (int)System.Math.Ceiling(path.SectionLengths[i] / 20);
                var start = path.Values[i];
                var delta = (path.Values[i + 1].position - start.position) / sl;
                var last = start.value;

                for (int t = 1; t <= sl + 1; ++t)
                {
                    var next = path.Evaluate(start.position + (t * delta));
                    var dir = Vector2.Normalize(next - last);
                    trail.Advance(last, dir);
                    last = next;
                }
            }
        }

        protected override bool HandleInput(GameTime time)
        {
            if (Takai.Input.InputState.IsPress(Takai.Input.MouseButtons.Left))
            {
                AddPathPoint(editor.Camera.ScreenToWorld(Takai.Input.InputState.MouseVector));
                return false;
            }

            return base.HandleInput(time);
        }

        void DrawPath(Takai.Game.VectorCurve path)
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
                editor.Map.DrawLine(last - new Vector2(3), last + new Vector2(3), Color.Cyan);
                editor.Map.DrawLine(last + new Vector2(-3, 3), last + new Vector2(3, -3), Color.Cyan);

                for (int t = 0; t < sl; ++t)
                {
                    var next = path.Evaluate(start.position + (t * delta));
                    editor.Map.DrawLine(last, next, Color.LightSeaGreen);
                    var p = Takai.Util.Ortho(Vector2.Normalize(next - last));
                    editor.Map.DrawLine(last - p * 5, last + p * 5, Color.Cyan);
                    last = next;
                }
            }

            editor.Map.DrawLine(last - new Vector2(3), last + new Vector2(3), Color.Cyan);
            editor.Map.DrawLine(last + new Vector2(-3, 3), last + new Vector2(3, -3), Color.Cyan);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            editor.Map.Spawn(trail);
            foreach (var path in paths)
                DrawPath(path);
            base.DrawSelf(spriteBatch);
        }
    }
}