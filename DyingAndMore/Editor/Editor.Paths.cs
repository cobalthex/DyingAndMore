using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class PathsConfigurator : Takai.Runtime.GameState
    {
        SpriteBatch sbatch;

        Takai.UI.Element uiContainer;

        public PathsConfigurator()
            : base(true, false) { }

        public override void Load()
        {
            sbatch = new SpriteBatch(GraphicsDevice);
            uiContainer = new Takai.UI.Element();
            uiContainer.AddChild(new Takai.UI.TextBox()
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
            paths = new List<Takai.Game.Path>
            {
                new Takai.Game.Path()
                {
                    ControlPoints = new List<Vector2>
                    {
                        new Vector2(200, 50),
                        new Vector2(300, 150),
                        new Vector2(400, 200),
                        new Vector2(500, 150),
                        new Vector2(600, 50),
                    }
                }
            };
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
                paths[0].ControlPoints.Add(editor.Map.ActiveCamera.ScreenToWorld(Takai.Input.InputState.MouseVector));
            }
        }

        void DrawPath(Takai.Game.Path path)
        {
            if (path.ControlPoints.Count > 0)
                editor.Map.DrawRect(new Rectangle((int)path.ControlPoints[0].X - 2, (int)path.ControlPoints[0].Y - 2, 4, 4), Color.White);

            for (int i = 0; i < path.ControlPoints.Count - 1; ++i)
            {
                var next = path.ControlPoints[i + 1];
                editor.Map.DrawLine(path.ControlPoints[i], next, Color.White);
                editor.Map.DrawRect(new Rectangle((int)next.X - 2, (int)next.Y - 2, 4, 4), Color.White);
            }
        }

        public override void Draw(SpriteBatch sbatch)
        {
            foreach (var path in paths)
                DrawPath(path);
        }
    }
}