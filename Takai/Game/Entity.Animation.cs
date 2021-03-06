﻿using System;
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
    public class AnimationClass : Data.INamedClass<AnimationInstance>
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
        /// Should the sprite always be drawn with the original sprite orientation?
        /// </summary>
        public bool AlwaysDrawUpright { get; set; } = false;

        public Light Light { get; set; }

        /// <summary>
        /// The name of the material for this animation. Mainly for physics
        /// </summary>
        public string Material { get; set; }

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
            Sprite != null ? Math.Max(Sprite.Width, Sprite.Height) / 2 : 1;

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

    //todo: redesign to work with serialization (e.g. CompletionCallback)

    public struct AnimationInstance : Data.IInstance<AnimationClass>, IDisposable
    {
        public AnimationClass Class
        {
            get => _class;
            set
            {
                _class = value;
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

    public partial class EntityClass
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

        [Data.Serializer.Ignored]
        public readonly Lazy<Graphics.Sprite> EditorPreviewSprite;

        //entity state map (id to class name)
    }

    public partial class EntityInstance
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
                if (baseAnimation.Class != null)
                    yield return baseAnimation;
                for (int i = 0; i < overlayAnimations.Count; ++i)
                    yield return overlayAnimations[i];
            }
        }

        public string Material => baseAnimation.Class?.Material;

        public bool IsPlayingAnyAnimations()
        {
            return baseAnimation.Class != null && overlayAnimations.Count > 0;
        }

        public bool IsPlayingAnimation(string animationName)
        {
            foreach (var animation in ActiveAnimations)
            {
                if (animation.Class.Name == animationName)
                    return true;
            }
            return false;
        }
        public bool IsPlayingAnimation(AnimationClass animationClass)
        {
            foreach (var animation in ActiveAnimations)
            {
                if (animation.Class == animationClass)
                    return true;
            }
            return false;
        }


        public bool PlayAnimation(string animation, Action completionCallback = null)
        {
            if (string.IsNullOrEmpty(animation) || Class.Animations == null || !Class.Animations.TryGetValue(animation, out var animClass))
            {
                //System.Diagnostics.Debug.WriteLine($"{this}: animation '{animation}' does not exist");
                completionCallback?.Invoke();
                return false;
            }

            return PlayAnimation(animClass, completionCallback);
        }

        /// <summary>
        /// Play a new animation. Does nothing if the animation is aleady being played
        /// </summary>
        /// <param name="animation">The animation to play</param>
        /// <param name="completionCallback">An optional callback called on completion of this animation. If the animation doesn't exist, the callback is called immediately</param>
        /// <returns>True if the animation was started, false if the animation was already playing or didn't exist</returns>
        public bool PlayAnimation(AnimationClass animation, Action completionCallback = null)
        {
            if (animation == null)
            {
                completionCallback?.Invoke();
                return false;
            }

            //todo: completion callback cant be serialized

            var instance = animation.Instantiate();
            instance.CompletionCallback = completionCallback;

            if (Map != null)
            {
                if (animation.EnterEffect != null)
                    Map.Spawn(animation.EnterEffect.Instantiate(this));

                if (animation.Sound != null)
                    Map.Spawn(animation.Sound.Instantiate(this));
            }
            if (animation.Type == AnimationType.Base)
            {
                if (baseAnimation.Class == animation)
                    return false;

                baseAnimation.CompletionCallback?.Invoke();
                baseAnimation.Dispose();
                baseAnimation = instance;
            }
            else
            {
                if (overlayAnimations.FindIndex((a) => a.Class == animation) >= 0)
                    return false;

                overlayAnimations.Add(instance);
            }

            Radius = Math.Max(Radius, animation.Radius);
            if (animation.Sprite != null)
                UpdateAxisAlignedBounds(); //todo

            return true;
        }

        public bool StopAnimation(string animation, bool invokeCallback = true)
        {
            if (Class.Animations == null || !Class.Animations.TryGetValue(animation, out var animClass))
                return false;

            return StopAnimation(animClass, invokeCallback);
        }

        /// <summary>
        /// Stop a playing animation
        /// Base animations will continue playing but call their completion callback
        /// </summary>
        /// <param name="animation">the animation to stop</param>
        /// <param name="invokeCallback">call the callback for the animation (if set)</param>
        /// <returns>True if the animation was stopped, false if the animation didn't exist or wasn't playing</returns>
        public bool StopAnimation(AnimationClass animation, bool invokeCallback = true)
        {
            if (animation.Type == AnimationType.Base && baseAnimation.Class == animation)
            {
                if (invokeCallback && baseAnimation.CompletionCallback != null)
                    baseAnimation.CompletionCallback();
                baseAnimation.CompletionCallback = null;
                baseAnimation.ElapsedTime = TimeSpan.Zero;

                if (Class.DefaultBaseAnimation != null)
                    PlayAnimation(Class.DefaultBaseAnimation);
            }
            else if (animation.Type == AnimationType.Overlay)
            {
                //todo: remove animation callbacks and make serializable

                var index = overlayAnimations.FindIndex((a) => a.Class == animation);
                if (index >= 0)
                {
                    if (invokeCallback && overlayAnimations[index].CompletionCallback != null)
                        overlayAnimations[index].CompletionCallback();
                    overlayAnimations[index].Dispose();
                    overlayAnimations.RemoveAt(index);
                }
            }
            else
                return false;

            if (animation.ExitEffect != null && Map != null)
                Map.Spawn(animation.ExitEffect.Instantiate(this));
            return true;
        }

        //todo: transition/completion events

        public virtual void UpdateAnimations(TimeSpan deltaTime)
        {
            Radius = 0;
            if (baseAnimation.Class != null)
            {
                Radius = baseAnimation.Class.Radius;

                bool wasFinished = baseAnimation.ElapsedTime > baseAnimation.Class.TotalTime;
                baseAnimation.ElapsedTime += deltaTime;
                if (!wasFinished && baseAnimation.ElapsedTime > baseAnimation.Class.TotalTime)
                {
                    baseAnimation.CompletionCallback?.Invoke();
                    baseAnimation.CompletionCallback = null;
                }

                if (baseAnimation.Class.Effect != null && Map != null)
                    Map.Spawn(baseAnimation.Class.Effect.Instantiate(this));
            }
            else // if (overlayAnimations.Count == 0)
                PlayAnimation(Class.DefaultBaseAnimation); //make sure there is always an animation playing

            for (int i = 0; i < overlayAnimations.Count; ++i)
            {
                var animation = overlayAnimations[i];
                animation.ElapsedTime += deltaTime;
                if (animation.ElapsedTime > animation.Class.TotalTime)
                {
                    if (animation.Class.ExitEffect != null && Map != null)
                        Map.Spawn(animation.Class.ExitEffect.Instantiate(this));
                    animation.CompletionCallback?.Invoke();
                    animation.Dispose();
                    overlayAnimations.RemoveAt(i);
                    --i;
                }
                else
                {
                    overlayAnimations[i] = animation;
                    Radius = Math.Max(Radius, animation.Class.Radius);
                    if (animation.Class != null)
                    {
                        if (baseAnimation.Class.Effect != null && Map != null)
                            Map.Spawn(baseAnimation.Class.Effect.Instantiate(this));
                    }
                }
            }
        }
    }
}