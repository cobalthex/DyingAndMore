using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public class ScalarFunction
    {
        public enum Type
        {
            Linear,
            Exponential,
            Logarithmic,
            Sinusoidal,
        }

        public float Scale { get; set; } = 1;

        public float Evaluate(float x)
        {
            return 0;
        }
    }

    public class TrailClass : IObjectClass<TrailInstance>
    {
        public string File { get; set; }
        public string Name { get; set; }

        public int MaxPoints { get; set; } = 10;
        public Graphics.Sprite Sprite { get; set; }
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Width across the length of the curve. Position 0 is the start (tail) of the trail
        /// </summary>
        public ScalarCurve Width { get; set; } = 1;

        /// <summary>
        /// Orthagonal jitter added to each point
        /// </summary>
        public Range<float> Jitter { get; set; } = 0;

        /// <summary>
        /// Zero for forever
        /// </summary>
        public TimeSpan LifeSpan { get; set; }

        /// <summary>
        /// Minimum time between adding new points (as not to have insane jitter with high fps)
        /// Zero for no delay
        /// </summary>
        public TimeSpan CaptureDelay { get; set; }

        /// <summary>
        /// Merge points that are colinear (when adding new points)
        /// </summary>
        public bool MergeCollinear { get; set; } = false;

        public TrailInstance Instantiate()
        {
            return new TrailInstance(this);
        }
    }

    public struct TrailPoint
    {
        public Vector2 location;
        public Vector2 direction;
        public TimeSpan time;

        public TrailPoint(Vector2 location, Vector2 direction, TimeSpan time)
        {
            this.location = location;
            this.direction = direction;
            this.time = time;
        }
    }

    public class TrailInstance : IObjectInstance<TrailClass>
    {
        //todo: make class readonly across board
        public TrailClass Class
        {
            get => _class;
            set
            {
                _class = value;
                if (_class != null && _class.MaxPoints > 0)
                    points.Resize(Class.MaxPoints);
                HeadIndex = TailIndex = Count = 0;
                elapsedTime = TimeSpan.Zero;
            }
        }
        private TrailClass _class;

        List<TrailPoint> points = new List<TrailPoint>();

        /// <summary>
        /// All of the points in this list, position and width
        /// </summary>
        public IReadOnlyList<TrailPoint> AllPoints => points;

        public IEnumerable<TrailPoint> Points
        {
            get
            {
                for (int i = 0; i < Count; ++i)
                    yield return points[(i + TailIndex) % points.Count];
            }
        }

        public int HeadIndex { get; private set; } = 0;
        public int TailIndex { get; private set; } = 0;
        public int Count { get; private set; } = 0;

        protected TimeSpan elapsedTime;
        protected TimeSpan nextCapture;

        public TrailInstance() : this(null) { }
        public TrailInstance(TrailClass @class)
        {
            Class = @class;
        }

        public void Update(TimeSpan deltaTime)
        {
            elapsedTime += deltaTime;

            if (Count > 0 &&
                Class.LifeSpan > TimeSpan.Zero &&
                elapsedTime - points[TailIndex].time > Class.LifeSpan)
            {
                TailIndex = (TailIndex + 1) % AllPoints.Count;
                --Count;
            }
        }

        bool IsCollinear(Vector2 a, Vector2 b, Vector2 c)
        {
            //test that slopes are the same
            return ((c.Y - b.Y) * (b.X - a.X)) == ((b.Y - a.Y) * (c.X - b.X));
        }

        /// <summary>
        /// Add a new point to the trail
        /// </summary>
        /// <param name="location">The next point of the trail</param>
        /// <param name="width">How wide the point is</param>
        /// <param name="collapse">Only add this point if its not on top of the last point</param>
        public void AddPoint(Vector2 location, Vector2 direction, bool collapse = true)
        {
            if ((collapse && Count > 0 && points[HeadIndex == 0 ? points.Count - 1 : HeadIndex - 1].location == location) ||
                elapsedTime < nextCapture)
                return;

            if (Class != null)
            {
                //todo: tangential jitter?

                location += direction.Ortho() * Class.Jitter.Random();
            }

            var p2 = (TailIndex + Count - 2) % points.Count;
            var p1 = (TailIndex + Count - 1) % points.Count;
            if (Class.MergeCollinear && Count > 2 &&
                IsCollinear(
                    location,
                    points[p1].location,
                    points[p2].location
                ))
            {

                points[p2] = new TrailPoint(points[p2].location, points[p2].direction, points[p1].time);
                points[p1] = new TrailPoint(location, direction, elapsedTime);
            }
            else if (Class.MaxPoints == 0)
            {
                points.Add(new TrailPoint(location, direction, elapsedTime));
                ++HeadIndex;
                ++Count;
            }
            else
            {
                points[HeadIndex] = new TrailPoint(location, direction, elapsedTime);
                HeadIndex = (HeadIndex + 1) % points.Count;

                if (Count >= points.Count)
                {
                    TailIndex = HeadIndex;
                    Count = points.Count;
                }
                else
                    ++Count;
            }

            nextCapture = elapsedTime + Class.CaptureDelay;
        }
    }
}
