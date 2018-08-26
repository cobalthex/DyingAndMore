using System;
using System.Collections.Generic;

namespace Takai.UI
{
    /// <summary>
    /// Applies an animation to a property
    /// </summary>
    public class Animation
    {
        public TimeSpan Duration { get; set; }
        public bool IsLooping { get; set; }

        public TimeSpan ElapsedTime { get; set; }

        public string TargetProperty { get; set; }

        //value curve
    }
}
