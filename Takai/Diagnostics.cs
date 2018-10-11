using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using P2D = Takai.Graphics.Primitives2D;

namespace Takai
{
    /// <summary>
    /// An append-only ring buffer
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    public class RingBuffer<T> : IReadOnlyList<T>
    {
        protected T[] entries = null;
        protected int next = 0;

        public int Count => entries.Length;

        public bool IsReadOnly => throw new NotImplementedException();

        public RingBuffer(int max)
        {
            entries = new T[max];
        }

        public virtual void Clear()
        {
            if (entries != null)
                entries = new T[entries.Length];
        }

        public virtual void Append(T entry)
        {
            entries[next] = entry;
            next = (next + 1) % entries.Length;
        }

        public T this[int index]
        {
            get => entries[index];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IReadOnlyList<T>)entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyList<T>)entries).GetEnumerator();
        }
    }

    public static class LogBuffer
    {
        public struct LogRow
        {
            public string text;
            public DateTime time;

            public LogRow(string text, DateTime time)
            {
                this.text = text;
                this.time = time;
            }
        }

        private static RingBuffer<LogRow> buffer = new RingBuffer<LogRow>(8);

        public static int Count => buffer.Count;

        public static IEnumerable<LogRow> Entries => buffer;

        public static void Clear()
        {
            buffer.Clear();
        }
        public static void Append(string text)
        {
            buffer.Append(new LogRow(text, DateTime.UtcNow));
        }
    }

    public class FpsGraph : UI.Static
    {
        struct FpsTick
        {
            public float fps;
            public TimeSpan time;

            public FpsTick(float fps, TimeSpan time)
            {
                this.fps = fps;
                this.time = time;
            }
        }

        RingBuffer<FpsTick> buffer;

        public float min = float.MaxValue;
        public float max = 0;
        public float sum = 60;
        int n = 1;

        public FpsGraph(int maxTicks = 256)
        {
            buffer = new RingBuffer<FpsTick>(maxTicks);
        }

        public void Clear()
        {
            buffer.Clear();
            min = float.MaxValue;
            max = 0;
        }

        protected override void UpdateSelf(GameTime time)
        {
            var tick = new FpsTick(1000 / (float)time.ElapsedGameTime.TotalMilliseconds, time.TotalGameTime);
            buffer.Append(tick);
            var avg = (sum / n);
            if (Math.Abs(tick.fps - avg) < avg * 2)
            {
                min = Math.Min(min, tick.fps);
                max = Math.Max(max, tick.fps);
                sum += tick.fps;
                ++n;
            } //todo: normalizing over time
            base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch sbatch)
        {
            var bounds = VisibleContentArea;
            var average = (sum / n);

            var min = 0;// average - average / 2;
            var max = 120;// average + average / 2;

            var smax = Font.MeasureString(max.ToString("N2"));
            Font.Draw(sbatch, max.ToString("N2"), bounds.Location.ToVector2(), Color.Aquamarine);
            var smin = Font.MeasureString(min.ToString("N2"));
            Font.Draw(sbatch, min.ToString("N2"), new Vector2(bounds.Left + smax.X - smin.X, bounds.Bottom - smin.Y), Color.Aquamarine);

            float dy = (max - min);

            float y = bounds.Bottom - ((sum / n) - min) / dy * bounds.Height;
            var savg = Font.MeasureString(average.ToString("N2"));
            Font.Draw(sbatch, average.ToString("N2"), new Vector2(bounds.Left + smax.X - savg.X, y - (savg.Y / 2)), Color.RoyalBlue);

            var advance = ((int)(smax.X - 1) / 5 + 1) * 5;
            bounds.X += advance + 5;
            bounds.Width -= advance + 5;
            P2D.DrawRect(sbatch, Color.White, bounds);
            bounds.Inflate(-5, -5);

            P2D.DrawLine(sbatch, Color.RoyalBlue, new Vector2(bounds.Left, y), new Vector2(bounds.Right, y));

            float step = (float)bounds.Width / buffer.Count;

            float x = bounds.Left;
            var last = new Vector2(x, bounds.Bottom - (average - min) / dy * bounds.Height);
            foreach (var entry in buffer)
            {
                if (entry.time == TimeSpan.Zero || entry.time == TimeSpan.Zero)
                    continue;

                y = bounds.Bottom - (entry.fps - min) / dy * bounds.Height;
                P2D.DrawLine(sbatch, Color.Black, last, new Vector2(x, y + 1));
                P2D.DrawLine(sbatch, Color.Black, new Vector2(x, y + 1), new Vector2(x + step, y + 1));
                P2D.DrawLine(sbatch, Color.Yellow, last, new Vector2(x, y));
                P2D.DrawLine(sbatch, Color.Yellow, new Vector2(x, y), new Vector2(x + step, y));
                last = new Vector2(x + step, y);

                x += step;
            }
        }
    }
}
