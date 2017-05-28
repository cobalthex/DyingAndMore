using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class PathsEditorMode : EditorMode
    {
        public static List<Takai.Game.Path> paths;

        Takai.UI.TrackBar trackBar;

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

            paths = new List<Takai.Game.Path>
            {
                new Takai.Game.Path()
            };
        }

        public override void Start()
        {
        }

        public override void End()
        {
        }

        protected override bool HandleInput(GameTime time)
        {
            if (Takai.Input.InputState.IsPress(Takai.Input.MouseButtons.Left))
            {
                paths[0].AddPoint(editor.Map.ActiveCamera.ScreenToWorld(Takai.Input.InputState.MouseVector));
                return false;
            }

            return base.HandleInput(time);
        }


        void DrawPath(Takai.Game.Path path)
        {
            int np = path.ControlPoints.Count;
            if (np < 1)
                return;

            //if (path.ControlPoints.Count > 0)
            //{
            //    editor.Map.DrawRect(new Rectangle((int)path.ControlPoints[0].X - 2, (int)path.ControlPoints[0].Y - 2, 4, 4), pathColor);

            //    editor.Map.DrawLine(path.ControlPoints[0], path.ControlPoints[1], pathColor);
            //    editor.Map.DrawLine(path.ControlPoints[path.ControlPoints.Count - 2], path.ControlPoints[path.ControlPoints.Count - 1], pathColor);
            //}

            //Vector2 GetPoint(float val, int segment)
            //{
            //    int c = path.ControlPoints.Count - 1;
            //    return CatmullRom(
            //        path.ControlPoints[MathHelper.Clamp(segment - 1, 0, c)],
            //        path.ControlPoints[MathHelper.Clamp(segment,     0, c)],
            //        path.ControlPoints[MathHelper.Clamp(segment + 1, 0, c)],
            //        path.ControlPoints[MathHelper.Clamp(segment + 2, 0, c)],
            //        1, val
            //    );
            //}

            //Vector2 last = GetPoint(0, 0);
            //for (int i = 0; i < path.SegmentLengths.Count; ++i)
            //{
            //    for (int s = 0; s <= path.SegmentLengths[i]; s += 5)
            //    {
            //        var next = GetPoint(MathHelper.Max(s, 0) / path.SegmentLengths[i], i + 1);
            //        editor.Map.DrawLine(last, next, Color.GreenYellow);
            //        last = next;
            //    }
            //}

            int nSegPoints = 15; //todo (based on segment length)

            double Interpolate(double p0, double p1, double p2, double p3, double[] time, double t)
            {
                double L01 = p0 * (time[1] - t) / (time[1] - time[0]) + p1 * (t - time[0]) / (time[1] - time[0]);
                double L12 = p1 * (time[2] - t) / (time[2] - time[1]) + p2 * (t - time[1]) / (time[2] - time[1]);
                double L23 = p2 * (time[3] - t) / (time[3] - time[2]) + p3 * (t - time[2]) / (time[3] - time[2]);
                double L012 = L01 * (time[2] - t) / (time[2] - time[0]) + L12 * (t - time[0]) / (time[2] - time[0]);
                double L123 = L12 * (time[3] - t) / (time[3] - time[1]) + L23 * (t - time[1]) / (time[3] - time[1]);
                double C12 = L012 * (time[2] - t) / (time[2] - time[1]) + L123 * (t - time[1]) / (time[2] - time[1]);
                return C12;
            }

            if (np > 2)
            {
                var p = new List<Vector2>(np + 2);

                if (path.ControlPoints[0] == path.ControlPoints[np - 1]) //is loop
                {
                    p.Add(path.ControlPoints[1]);
                    p.AddRange(path.ControlPoints);
                    p.Add(path.ControlPoints[np - 2]);
                }
                else
                {
                    //extrapolate start and end points
                    p.Add(path.ControlPoints[0] - (path.ControlPoints[1] - path.ControlPoints[0]));
                    p.AddRange(path.ControlPoints);
                    p.Add(path.ControlPoints[np - 1] + (path.ControlPoints[np - 1] - path.ControlPoints[np - 2]));
                }

                var result = new List<Vector2>();
                //i + 1

                float alpha = trackBar.NormalizedValue; //curve parameterization (α)

                var times = new double[4];
                for (int i = 0; i < p.Count - 3; ++i)
                {
                    double tStart = 1;
                    double tEnd = 2;


                    if (alpha != 0)
                    {
                        double total = 0;
                        times[0] = i;
                        for (int j = 1; j < 4; ++j)
                        {
                            var d = p[j + i] - p[j + i - 1];
                            total += System.Math.Pow(d.LengthSquared(), alpha);
                            times[j] = total;
                        }
                        tStart = times[1];
                        tEnd = times[2];
                    }
                    else
                    {
                        for (int j = 0; j < times.Length; ++j)
                            times[j] = i + j;
                    }

                    for (int j = 1; j < nSegPoints - 1; ++j)
                    {
                        var t = tStart + (j * (tEnd - tStart)) / (nSegPoints - 1);
                        result.Add(new Vector2(
                            (float)Interpolate(p[i].X, p[i + 1].X, p[i + 2].X, p[i + 3].X, times, t),
                            (float)Interpolate(p[i].Y, p[i + 1].Y, p[i + 2].Y, p[i + 3].Y, times, t)
                        ));
                    }
                }

                //result.Add(path.ControlPoints[i + 2]);

                for (int i = 1; i < result.Count; ++i)
                    editor.Map.DrawLine(result[i - 1], result[i], Color.GreenYellow);
            }
            else if (np > 1)
            {
                for (int i = 1; i < np; ++i)
                    editor.Map.DrawLine(path.ControlPoints[i - 1], path.ControlPoints[i], Color.GreenYellow);
            }

            //draw directional arrows at control points
            foreach (var cp in path.ControlPoints)
            {
                //var tail = new Vector2()
                editor.Map.DrawLine(cp - new Vector2(3), cp + new Vector2(3), Color.Cyan);
                editor.Map.DrawLine(cp + new Vector2(-3, 3), cp + new Vector2(3, -3), Color.Cyan);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            foreach (var path in paths)
                DrawPath(path);
        }
    }
}