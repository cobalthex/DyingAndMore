namespace Takai
{
    /// <summary>
    /// A flexible animation system 
    /// </summary>
    public class Animation : System.Diagnostics.Stopwatch
    {
        /// <summary>
        /// The number of frames in this animation
        /// Use 0 for non frame-based animation
        /// </summary>
        public int FrameCount { get; set; } = 0;
        /// <summary>
        /// Length of each frame
        /// If not using frames, length of whole animation
        /// </summary>
        public System.TimeSpan FrameTime { get; set; }

        /// <summary>
        /// Loop the animation (only applies when using currentFrame)
        /// </summary>
        public bool IsLooping { get; set; } = true;

        /// <summary>
        /// An optional offset to add to the time (only when checking currentFrame)
        /// </summary>
        public System.TimeSpan TimeOffset { get; set; } = System.TimeSpan.Zero;

        /// <summary>
        /// Get or set the current frame with offset calculated
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                if (FrameCount == 0 || FrameTime == System.TimeSpan.Zero)
                    return 0;

                var f = (int)((Elapsed - TimeOffset).Ticks / FrameTime.Ticks);

                if (IsLooping)
                    return f % FrameCount;
                else
                {
                    if (f >= FrameCount)
                    {
                        Stop();
                        return FrameCount;
                    }
                    else
                        return f;
                }
            }
            set
            {
                if (FrameCount == 0)
                    return;

                var val = value % FrameCount;
                var cf = (int)((Elapsed - TimeOffset).Ticks / FrameTime.Ticks);

                if (val == 0)
                    Restart();
                else
                    TimeOffset = System.TimeSpan.FromTicks((val - cf) * FrameTime.Ticks);
            }
        }

        /// <summary>
        /// Get the fractional value of the current frame (0 to 1)
        /// </summary>
        public float FrameDelta
        {
            get
            {
                var f = (float)(Elapsed - TimeOffset).Ticks / (float)FrameTime.Ticks;
                return f - (int)f;
            }
            //todo: add setter that updates offset to current frame + frame delta
        }

        /// <summary>
        /// Get the current frame + fractional frame
        /// </summary>
        public float CurrentFrameDelta
        {
            get
            {
                if (FrameCount == 0 || FrameTime == System.TimeSpan.Zero)
                    return 0;

                var f = (float)(Elapsed - TimeOffset).Ticks / (float)FrameTime.Ticks;

                if (IsLooping)
                    return f % FrameCount;
                else
                {
                    if (f >= FrameCount)
                    {
                        Stop();
                        return FrameCount;
                    }
                    else
                        return f;
                }
            }
            set
            {
                if (FrameCount == 0)
                    return;

                var val = value % FrameCount;
                var cf = (uint)((Elapsed - TimeOffset).Ticks / FrameTime.Ticks);

                if (val == 0)
                    Restart();
                else
                    TimeOffset = System.TimeSpan.FromTicks((uint)((val - cf) * FrameTime.Ticks));
            }
        }

        /// <summary>
        /// Create a new animation
        /// </summary>
        /// <param name="FrameCount">THe number of frames, 0 for non frame-based animation</param>
        /// <param name="FrameTime">The length of each frame, however, if frameCount = 0, length of whole animation</param>
        /// <param name="ShouldLoop">Should the animation loop</param>
        /// <param name="StartImmediately"></param>
        public Animation(int FrameCount, System.TimeSpan FrameTime, bool ShouldLoop, bool StartImmediately)
        {
            this.FrameCount = FrameCount;
            this.FrameTime = FrameTime;
            this.IsLooping = ShouldLoop;

            if (StartImmediately)
                base.Start();
        }

        /// <summary>
        /// Create a new animation
        /// </summary>
        public Animation()
        {
            FrameCount = 0;
            FrameTime = System.TimeSpan.Zero;
            IsLooping = false;
        }

        public Animation Clone()
        {
            return (Animation)this.MemberwiseClone();
        }

        public new void Restart()
        {
            TimeOffset = System.TimeSpan.Zero;
            base.Restart();
        }
        
        /// <summary>
        /// Is the animation at the end (and not looping)
        /// </summary>
        /// <returns>True if at the end</returns>
        public bool IsFinished()
        {
            return !IsRunning && (CurrentFrame >= FrameCount);
        }
    }
}
