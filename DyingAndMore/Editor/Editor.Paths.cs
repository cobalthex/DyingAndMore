﻿using System.Collections.Generic;
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
                var path = new Takai.Game.Path();
                path.AddPoint(new Vector2(200, 50));
                path.AddPoint(new Vector2(300, 150));
                path.AddPoint(new Vector2(400, 200));
                path.AddPoint(new Vector2(500, 150));
                path.AddPoint(new Vector2(600, 50));
                paths = new List<Takai.Game.Path>
                {
                    path
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