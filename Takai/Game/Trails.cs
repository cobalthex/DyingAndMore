using System;
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

        public TrailPoint(Vector2 location, float width)
        {
            this.location = location;
            this.width = width;
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

        public int Start { get; private set; } = 0;

        public void AddPoint(Vector2 point, float width)
        {
            if (Class.MaxPoints == 0)
            {
                points.Add(new TrailPoint(point, width));
                ++Start;
            }
            else
            {
                if (points.Count <= Start)
                {
                    points.Capacity = Class.MaxPoints;
                    points.Add(new TrailPoint(point, width));
                }
                else
                    points[Start] = new TrailPoint(point, width);

                Start = (Start + 1) % Class.MaxPoints;
            }
        }
    }
}
