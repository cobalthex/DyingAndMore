﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public class TrailClass : IObjectClass<TrailInstance>
    {
        public string File { get; set; }
        public string Name { get; set; }

        public int MaxPoints { get; set; } = 10;
        public Graphics.Sprite Sprite { get; set; }
        public Color Color { get; set; } = Color.White;

        //fade?
        public bool AutoTaper { get; set; }
        //taper middle (curve scalar?)

        /// <summary>
        /// Zero for forever
        /// </summary>
        public TimeSpan Lifetime { get; set; }

        public TrailInstance Instantiate()
        {
            return new TrailInstance
            {
                Class = this
            };
        }
    }

    public struct TrailPoint
    {
        public Vector2 location;
        public float width;
        public TimeSpan time;

        public TrailPoint(Vector2 location, float width, TimeSpan time)
        {
            this.location = location;
            this.width = width;
            this.time = time;
        }
    }

    public class TrailInstance : IObjectInstance<TrailClass>
    {
        public TrailClass Class { get; set; }

        List<TrailPoint> points = new List<TrailPoint>();

        /// <summary>
        /// All of the points in this list, position and width
        /// </summary>
        public IReadOnlyList<TrailPoint> Points => points;

        public int HeadIndex { get; private set; } = 0;
        public int TailIndex { get; private set; } = 0;

        protected TimeSpan elapsedTime;

        public bool IsEmpty()
        {
            return HeadIndex == TailIndex;
        }

        public void Update(TimeSpan deltaTime)
        {
            elapsedTime += deltaTime;

            if (HeadIndex != TailIndex &&
                Class.Lifetime > TimeSpan.Zero &&
                elapsedTime - points[TailIndex].time > Class.Lifetime)
                TailIndex = (TailIndex + 1) % Points.Count;
        }

        public void AddPoint(Vector2 point, float width)
        {
            if (Class.MaxPoints == 0)
            {
                points.Add(new TrailPoint(point, width, Class.Lifetime));
                ++HeadIndex;
            }
            else
            {
                if (points.Count <= HeadIndex)
                {
                    points.Capacity = Class.MaxPoints;
                    points.Add(new TrailPoint(point, width, Class.Lifetime));
                }
                else
                    points[HeadIndex] = new TrailPoint(point, width, Class.Lifetime);

                HeadIndex = (HeadIndex + 1) % Class.MaxPoints;
                if (HeadIndex >= TailIndex)
                    TailIndex = (HeadIndex + 1) % points.Count;
            }
        }
    }
}
