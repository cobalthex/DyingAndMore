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

        public float Width { get; set; } = 1;

        public bool AutoTaper { get; set; }
        //todo: width curve?

        /// <summary>
        /// Orthagonal jitter added to each point
        /// </summary>
        public Range<float> Jitter { get; set; } = 0;

        /// <summary>
        /// Zero for forever
        /// </summary>
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// Minimum time between adding new points (as not to have insane jitter with high fps)
        /// Zero for no delay
        /// </summary>
        public TimeSpan CaptureDelay { get; set; }

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
                Class.Lifetime > TimeSpan.Zero &&
                elapsedTime - points[TailIndex].time > Class.Lifetime)
            {
                TailIndex = (TailIndex + 1) % AllPoints.Count;
                --Count;
            }
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

            if (Class.MaxPoints == 0)
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
