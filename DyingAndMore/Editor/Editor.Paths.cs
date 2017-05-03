using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class PathsConfigurator : Takai.Runtime.GameState
    {
        SpriteBatch sbatch;

        Takai.UI.Static uiContainer;

        public PathsConfigurator()
            : base(true, false) { }

        public override void Load()
        {
            sbatch = new SpriteBatch(GraphicsDevice);
            uiContainer = new Takai.UI.Static();
            uiContainer.AddChild(new Takai.UI.TextInput()
            {
                Text = "Test",
                Position = new Vector2(10, 10)
            });
        }

        public override void Update(GameTime time)
        {
            uiContainer.Update(time);
        }

        public override void Draw(GameTime time)
        {
            sbatch.Begin();
            uiContainer.Draw(sbatch);
            sbatch.End();
        }
    }

    class PathsEditorMode : EditorMode
    {
        PathsConfigurator configurator;

        public static List<Takai.Game.Path> paths;

        public PathsEditorMode(Editor editor)
            : base("Paths", editor)
        {
            if (paths == null)
            {
                paths = new List<Takai.Game.Path>
                {
                    new Takai.Game.Path()
                };
            }
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
        }

        public override void Start()
        {
        }

        public override void End()
        {
        }

        public override void Update(GameTime time)
        {
            if (Takai.Input.InputState.IsPress(Takai.Input.MouseButtons.Left))
            {
                paths[0].AddPoint(editor.Map.ActiveCamera.ScreenToWorld(Takai.Input.InputState.MouseVector));
            }
        }

        void DrawPath(Takai.Game.Path path)
        {
            if (path.ControlPoints.Count < 1)
                return;

            //if (path.ControlPoints.Count > 0)
            //{
            //    editor.Map.DrawRect(new Rectangle((int)path.ControlPoints[0].X - 2, (int)path.ControlPoints[0].Y - 2, 4, 4), pathColor);

            //    editor.Map.DrawLine(path.ControlPoints[0], path.ControlPoints[1], pathColor);
            //    editor.Map.DrawLine(path.ControlPoints[path.ControlPoints.Count - 2], path.ControlPoints[path.ControlPoints.Count - 1], pathColor);
            //}

            Vector2 GetPoint(float val, int segment)
            {
                int c = path.ControlPoints.Count - 1;
                return Vector2.CatmullRom(
                    path.ControlPoints[MathHelper.Clamp(segment - 1, 0, c)],
                    path.ControlPoints[MathHelper.Clamp(segment,     0, c)],
                    path.ControlPoints[MathHelper.Clamp(segment + 1, 0, c)],
                    path.ControlPoints[MathHelper.Clamp(segment + 2, 0, c)],
                    val
                );
            }

            Vector2 last = GetPoint(0, 0);
            for (int i = 0; i < path.SegmentLengths.Count; ++i)
            {
                for (int s = 0; s <= path.SegmentLengths[i]; s += 5)
                {
                    var next = GetPoint(MathHelper.Max(s, 0) / path.SegmentLengths[i], i + 1);
                    editor.Map.DrawLine(last, next, Color.GreenYellow);
                    last = next;
                }
            }

            //draw directional arrows at control points
            foreach (var p in path.ControlPoints)
            {
                editor.Map.DrawLine(p - new Vector2(3), p + new Vector2(3), Color.Cyan);
                editor.Map.DrawLine(p + new Vector2(-3, 3), p + new Vector2(3, -3), Color.Cyan);
            }
        }

        public override void Draw(SpriteBatch sbatch)
        {
            foreach (var path in paths)
                DrawPath(path);
        }
    }
}