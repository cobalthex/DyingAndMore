using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public enum AnimationType
    {
        Base, //looping
        Overlay //removed after completion
    }

    /// <summary>
    /// Forms the base of any state in the state machine
    /// </summary>
    public class AnimationClass : IObjectClass<AnimationInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// A unique name for this state
        /// </summary>
        public string Name { get; set; }

        public AnimationType Type { get; set; }

        //todo: sprite loop frame?
        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// Looping state sound
        /// </summary>
        public SoundClass Sound { get; set; }

        public EffectsClass EnterEffect { get; set; }
        public EffectsClass ExitEffect { get; set; }

        /// <summary>
        /// An effect to play (repeatedly) while the state is active
        /// </summary>
        public EffectsClass Effect { get; set; }

        public float Radius =>
            Sprite != null ? MathHelper.Max(Sprite.Width, Sprite.Height) / 2 : 1;

        [Data.Serializer.Ignored]
        public TimeSpan TotalTime => Sprite?.TotalLength ?? TimeSpan.Zero;

        public AnimationInstance Instantiate()
        {
            return new AnimationInstance()
            {
                Class = this,
            };
        }
    }

    public struct AnimationInstance : IObjectInstance<AnimationClass>, IDisposable
    {
        public AnimationClass Class
        {
            get => _class;
            set
            {
                _class = value;

                if (_class != null)
                {
                    Sound.Instance?.Dispose();
                    if (_class.Sound != null)
                    {
                        Sound = _class.Sound.Instantiate();
                        if (Sound.Instance != null)
                        {
                            Sound.Instance.IsLooped = Class.Type == AnimationType.Base;
                            Sound.Instance.Play();
                        }
                    }
                }
            }
        }
        private AnimationClass _class;

        [Data.Serializer.Ignored]
        public SoundInstance Sound { get; set; }

        public TimeSpan ElapsedTime { get; set; }

        [Data.Serializer.Ignored]
        public Action CompletionCallback { get; set; }

        public void Dispose()
        {
            Sound.Instance?.Dispose();
        }

        public override string ToString()
        {
            return $"{Class?.Name}";
        }
    }

    public class StateCompleteEventArgs : EventArgs
    {
        public AnimationInstance State { get; set; }
    }

    public class TransitionEventArgs : EventArgs
    {
        public AnimationInstance PreviousState { get; set; }
        public AnimationInstance NextState { get; set; }
    }

    public abstract partial class EntityClass
    {
        /// <summary>
        /// All available animations
        /// </summary>
        public Dictionary<string, AnimationClass> Animations { get; set; }

        /// <summary>
        /// The default base animation set for when there is no other base state
        /// Set to null to leave the previous state
        /// </summary>
        public string DefaultBaseAnimation { get; set; } = "Idle";

        //entity state map (id to class name)
    }

    public abstract partial class EntityInstance
    {
        AnimationInstance baseAnimation;
        List<AnimationInstance> overlayAnimations = new List<AnimationInstance>();

        /// <summary>
        /// All of the active animations, base animatino first
        /// </summary>
        public IEnumerable<AnimationInstance> ActiveAnimations
        {
            get
            {
                yield return baseAnimation;
                for (int i = 0; i < overlayAnimations.Count; ++i)
                    yield return overlayAnimations[i];
            }
        }

        /// <summary>
        /// Play a new animation. Does nothing if the animation is aleady being played
        /// </summary>
        /// <param name="animation">The animation to play</param>
        /// <param name="completionCallback">An optional callback called on completion of this animation. If the animation doesn't exist, the callback is called immediately</param>
        /// <returns>True if the animation was started, false if the animation was already playing or didn't exist</returns>
        public bool PlayAnimation(string animation, Action completionCallback = null)
        {
            if (Class.Animations != null && Class.Animations.TryGetValue(animation, out var animClass))
            {
                var instance = animClass.Instantiate();
                instance.CompletionCallback = completionCallback;

                if (animClass.Type == AnimationType.Base)
                {
                    if (baseAnimation.Class == animClass)
                        return false;

                    baseAnimation.CompletionCallback?.Invoke();
                    baseAnimation.Dispose();
                    baseAnimation = instance;
                }
                else
                {
                    if (overlayAnimations.FindIndex((a) => a.Class == animClass) >= 0)
                        return false;

                    overlayAnimations.Add(instance);
                }

                if (animClass.EnterEffect != null && Map != null)
                    Map.Spawn(animClass.EnterEffect.Create(this));

                Radius = MathHelper.Max(Radius, animClass.Radius);
                if (animClass.Sprite != null)
                {
                    lastVisibleSize = Util.Max(lastVisibleSize, animClass.Sprite.Size);
                    UpdateAxisAlignedBounds();
                }

                return true;
            }
            else
                completionCallback?.Invoke();
            return false;
        }

        /// <summary>
        /// Stop a playing animation
        /// Base animations will continue playing but call their completion callback
        /// </summary>
        /// <param name="animation">the animation to stop</param>
        /// <param name="callCallback">call the callback for the animation (if set)</param>
        /// <returns>True if the animation was stopped, false if the animation didn't exist or wasn't playing</returns>
        public bool StopAnimation(string animation, bool callCallback = true)
        {
            if (Class.Animations == null || !Class.Animations.TryGetValue(animation, out var animClass))
                return false;

            if (animClass.Type == AnimationType.Base && baseAnimation.Class == animClass)
            {
                if (callCallback && baseAnimation.CompletionCallback != null)
                    baseAnimation.CompletionCallback();
                baseAnimation.CompletionCallback = null;

                if (Class.DefaultBaseAnimation != null)
                    PlayAnimation(Class.DefaultBaseAnimation);
            }
            else if (animClass.Type == AnimationType.Overlay)
            {
                var index = overlayAnimations.FindIndex((a) => a.Class == animClass);

                if (callCallback && overlayAnimations[index].CompletionCallback != null)
                    overlayAnimations[index].CompletionCallback();
                overlayAnimations[index].Dispose();
                overlayAnimations.RemoveAt(index);
            }
            else
                return false;

            if (animClass.ExitEffect != null && Map != null)
                Map.Spawn(animClass.ExitEffect.Create(this));
            return true;
        }

        //todo: transition/completion events

        public virtual void UpdateAnimations(TimeSpan deltaTime)
        {
            bool wasFinished = baseAnimation.ElapsedTime > baseAnimation.Class.TotalTime;
            baseAnimation.ElapsedTime += deltaTime;
            if (!wasFinished && baseAnimation.ElapsedTime > baseAnimation.Class.TotalTime)
            {
                baseAnimation.CompletionCallback?.Invoke();
                baseAnimation.CompletionCallback = null;
            }

            Radius = 0;
            lastVisibleSize = Point.Zero;
            if (baseAnimation.Class != null)
            {
                Radius = baseAnimation.Class.Radius;

                if (baseAnimation.Class.Sprite != null)
                    lastVisibleSize = baseAnimation.Class.Sprite.Size;

                if (baseAnimation.Class.Effect != null && Map != null)
                    Map.Spawn(baseAnimation.Class.Effect.Create(this));
            }

            for (int i = 0; i < overlayAnimations.Count; ++i)
            {
                var animation = overlayAnimations[i];
                animation.ElapsedTime += deltaTime;
                if (animation.ElapsedTime > animation.Class.TotalTime)
                {
                    if (animation.Class.ExitEffect != null && Map != null)
                        Map.Spawn(animation.Class.ExitEffect.Create(this));
                    animation.CompletionCallback?.Invoke();
                    animation.Dispose();
                    overlayAnimations.RemoveAt(i);
                    --i;
                }
                else
                {
                    overlayAnimations[i] = animation;
                    Radius = MathHelper.Max(Radius, baseAnimation.Class.Radius);
                    if (baseAnimation.Class != null)
                    {
                        if (baseAnimation.Class.Sprite != null)
                            lastVisibleSize = Util.Max(lastVisibleSize, baseAnimation.Class.Sprite.Size);

                        if (baseAnimation.Class.Effect != null && Map != null)
                            Map.Spawn(baseAnimation.Class.Effect.Create(this));
                    }
                }
            }
        }
    }
}


//death events (drop weapon, etc)

/*
Animation completion events
    event time->handlerFn (called in Think())

PlayAnimationAndWait(animation, callback)

*/