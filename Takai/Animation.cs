namespace Takai
{
    /// <summary>
    /// A simple enumeration that allows for customization of animations when loading
    /// </summary>
    [System.Flags]
    public enum AnimationOptions
    {
        None,
        Loop,
        StartImmediately,
        All = Loop | StartImmediately
    }

    /// <summary>
    /// A flexible animation system 
    /// </summary>
    public class Animation : System.Diagnostics.Stopwatch
    {
        /// <summary>
        /// The number of frames in this animation
        /// Use 0 for non frame-based animation
        /// </summary>
        public uint frameCount = 0;
        /// <summary>
        /// Length of each frame
        /// If not using frames, length of whole animation
        /// </summary>
        public System.TimeSpan frameLength;

        /// <summary>
        /// Loop the animation (only applies when using currentFrame)
        /// </summary>
        public bool isLooping = true;

        /// <summary>
        /// An optional offset to add to the time (only when checking currentFrame)
        /// </summary>
        public System.TimeSpan offset = System.TimeSpan.Zero;

        /// <summary>
        /// Get or set the current frame with offset calculated
        /// </summary>
        public uint currentFrame
        {
            get
            {
                if (frameCount == 0 || frameLength == System.TimeSpan.Zero)
                    return 0;

                uint f = (uint)((Elapsed - offset).Ticks / frameLength.Ticks);

                if (isLooping)
                    return f % frameCount;
                else
                {
                    if (f >= frameCount)
                    {
                        Stop();
                        return frameCount;
                    }
                    else
                        return f;
                }
            }
            set
            {
                if (frameCount == 0)
                    return;

                uint val = value % frameCount;
                uint cf = (uint)((Elapsed - offset).Ticks / frameLength.Ticks);

                if (val == 0)
                    Restart();
                else
                    offset = System.TimeSpan.FromTicks((val - cf) * frameLength.Ticks);
            }
        }

        /// <summary>
        /// Create a new animation
        /// </summary>
        /// <param name="frameCount">THe number of frames, 0 for non frame-based animation</param>
        /// <param name="frameLength">The length of each frame, however, if frameCount = 0, length of whole animation</param>
        /// <param name="Loop">Loop the animation</param>
        public Animation(uint FrameCount, System.TimeSpan FrameLength, AnimationOptions Options)
        {
            frameCount = FrameCount;
            frameLength = FrameLength;
            isLooping = (Options & AnimationOptions.Loop) > 0;

            if ((Options & AnimationOptions.StartImmediately) > 0)
                base.Start();
        }

        /// <summary>
        /// Create a new animation
        /// </summary>
        public Animation()
        {
            frameCount = 0;
            frameLength = System.TimeSpan.Zero;
            isLooping = false;
        }

#if XBOX || WINDOWS_PHONE
        /// <summary>
        /// Restart the animation
        /// (Used for compatability with Xbox)
        /// </summary>
        public void Restart()
        {
            offset = System.TimeSpan.Zero;
            Reset();
            Start();
        }
#else
        public new void Restart()
        {
            offset = System.TimeSpan.Zero;
            base.Restart();
        }
#endif
        
        /// <summary>
        /// Is the animation at the end (and not looping)
        /// </summary>
        /// <returns>True if at the end</returns>
        public bool IsFinished()
        {
            return !IsRunning && (currentFrame >= frameCount);
        }
    }
}
