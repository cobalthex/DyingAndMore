﻿using System;
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
    public class RingBuffer<T> : IEnumerable<T>
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
            for (int i = 0; i < entries.Length; ++i)
                yield return entries[(i + next) % entries.Length];
        }
        public IEnumerator<T> GetReverseEnumerator()
        {
            for (int i = entries.Length - 1; i >= 0; --i)
                yield return entries[(i + next) % entries.Length];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < entries.Length; ++i)
                yield return entries[i + next];
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

        private static RingBuffer<LogRow> buffer = new RingBuffer<LogRow>(16);

        public static int Count => buffer.Count;

        public static IEnumerable<LogRow> Entries => buffer;

        public static IEnumerator<LogRow> GetEntriesReversed() => buffer.GetReverseEnumerator();

        public static void Clear()
        {
            buffer.Clear();
        }

        public static void Append(params object[] o) => Append(string.Join(" ", o));
        public static void Append(string text)
        {
#if DEBUG
            buffer.Append(new LogRow(text, DateTime.UtcNow));
#endif
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

        public float Average { get; private set; }

        /// <summary>
        /// How often to sample (maximum sample rate, may be lower if Update is called less often)
        /// </summary>
        public TimeSpan SampleRate { get; set; } = TimeSpan.FromMilliseconds(10);
        TimeSpan lastSampleTime = TimeSpan.MinValue;

        public FpsGraph()
            : this(256) { }

        public FpsGraph(int maxTicks)
        {
            buffer = new RingBuffer<FpsTick>(maxTicks);
        }

        public void Clear()
        {
            buffer.Clear();
        }

        protected override void UpdateSelf(GameTime time)
        {
            base.UpdateSelf(time);

            if (time.TotalGameTime < lastSampleTime + SampleRate)
                return;

            var tick = new FpsTick(1000 / (float)time.ElapsedGameTime.TotalMilliseconds, time.TotalGameTime);
            buffer.Append(tick);

            var diff = (tick.fps - Average) / buffer.Count;
            Average += diff;

            lastSampleTime = time.TotalGameTime;
        }

        protected override void DrawSelf(UI.DrawContext context)
        {
            var bounds = VisibleContentArea;
            var average = Average;

            var min = average - average / 2;
            var max = average + average / 2;

            //todo: rewrite

            var smax = Font.MeasureString(max.ToString("N2"), TextStyle);
            var drawText = new Graphics.DrawTextOptions(
                max.ToString("N2"),
                Font,
                TextStyle,
                Color.RoyalBlue,
                bounds.Location.ToVector2()
            );
            context.textRenderer.Draw(drawText);

            drawText.text = min.ToString("N2");
            var smin = Font.MeasureString(drawText.text, TextStyle);
            drawText.position = new Vector2(bounds.Left + smax.X - smin.X, bounds.Bottom - smin.Y);
            context.textRenderer.Draw(drawText);

            float dy = (max - min);

            drawText.text = average.ToString("N2");
            var savg = Font.MeasureString(drawText.text, TextStyle);
            float y = bounds.Bottom - ((dy / buffer.Count) - min) / dy * bounds.Height;
            drawText.position = new Vector2(bounds.Left + smax.X - savg.X, bounds.Top + (bounds.Height - savg.Y) / 2);
            context.textRenderer.Draw(drawText);

            var advance = ((int)(smax.X - 1) / 5 + 1) * 5;
            bounds.X += advance + 5;
            bounds.Width -= advance + 5;
            P2D.DrawRect(context.spriteBatch, Color.White, bounds);
            bounds.Inflate(-5, -5);

            P2D.DrawLine(context.spriteBatch, Color.Azure, new Vector2(bounds.Left, bounds.Center.Y), new Vector2(bounds.Right, bounds.Center.Y));

            float step = (float)bounds.Width / buffer.Count;

            float x = bounds.Left;
            var last = new Vector2(x, bounds.Bottom - (average - min) / dy * bounds.Height);

            foreach (var entry in buffer)
            {
                if (entry.time == TimeSpan.Zero || entry.time == TimeSpan.Zero)
                    continue;

                y = bounds.Bottom - (entry.fps - min) / dy * bounds.Height;
                P2D.DrawLine(context.spriteBatch, Color.Black, last, new Vector2(x, y + 1));
                P2D.DrawLine(context.spriteBatch, Color.Black, new Vector2(x, y + 1), new Vector2(x + step, y + 1));
                P2D.DrawLine(context.spriteBatch, Color.Yellow, last, new Vector2(x, y));
                P2D.DrawLine(context.spriteBatch, Color.Yellow, new Vector2(x, y), new Vector2(x + step, y));
                last = new Vector2(x + step, y);

                x += step;
            }
        }
    }
}
