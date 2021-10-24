using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Stylesheet = System.Collections.Generic.Dictionary<string, object>;

namespace Takai.UI
{
    public struct Animation
    {
        public Animator animator;
        public object argument;
        // animation settings
    }

    /// <summary>
    /// An animation coroutine. Called once per update frame (while enabled) 
    /// </summary>
    /// <param name="target">The UI element this is animating on</param>
    /// <param name="time">Game time</param>
    /// <param name="argument">Optional argument(s) passed to the animation. animation specific</param>
    /// <returns>The current state of the animation</returns>
    public delegate IEnumerator Animator(Static target, GameTime time, object argument);

    public partial class Static
    {
        private Queue<Animation> queuedAnimations;
        private List<IEnumerator> activeAnimations; // unordered

        public void Animate(Animation animation)
        {
            if (queuedAnimations == null)
                queuedAnimations = new Queue<Animation>();

            queuedAnimations.Enqueue(animation);
        }

        /// <summary>
        /// Transition to a new style
        /// </summary>
        /// <param name="newStyle">The name of the new style (including state)</param>
        /// <param name="duration">How long to transition for</param>
        public void TransitionStyleTo(string newStyle, TimeSpan duration)
        {
            if (newStyle == Styles) return; // already there
            
            // cache active transition and lerp between the existing state and new state?

            if (queuedAnimations == null)
                queuedAnimations = new Queue<Animation>();

            queuedAnimations.Enqueue(new Transition
            {
                startStyle = GenerateStyleSheet<StyleSheet>(),
                // TODO
                //finishStyle = GenerateStyleSheet<StyleSheet>(newStyle, styleStates),
                duration = duration,
            }.GetAnimation());
        }
    }

    struct Transition
    {
        public StyleSheet startStyle;
        public StyleSheet finishStyle;
        public TimeSpan duration;

        public Animation GetAnimation()
        {
            var self = this;
            IEnumerator anim8or(Static target, GameTime time, object arg)
            {
                var startTime = time.TotalGameTime;
                var finishTime = startTime + self.duration;

                while (time.TotalGameTime < finishTime)
                {
                    var style = self.startStyle;
                    style.LerpWith(self.finishStyle, (float)((time.TotalGameTime - startTime).TotalSeconds / self.duration.TotalSeconds));

                    // TESTING
                    if (style.Color.HasValue) target.Color = style.Color.Value;
                    if (style.Font != null) target.Font = style.Font;
                    if (style.TextStyle.HasValue) target.TextStyle = style.TextStyle.Value;
                    if (style.BorderColor.HasValue) target.BorderColor = style.BorderColor.Value;
                    if (style.BackgroundColor.HasValue) target.BackgroundColor = style.BackgroundColor.Value;
                    if (style.BackgroundSprite.HasValue) target.BackgroundSprite = style.BackgroundSprite.Value;
                    if (style.HorizontalAlignment.HasValue) target.HorizontalAlignment = style.HorizontalAlignment.Value;
                    if (style.VerticalAlignment.HasValue) target.VerticalAlignment = style.VerticalAlignment.Value;
                    if (style.Position.HasValue) target.Position = style.Position.Value;
                    if (style.Size.HasValue) target.Size = style.Size.Value;
                    if (style.Padding.HasValue) target.Padding = style.Padding.Value;

                    yield return null;
                }


                System.Diagnostics.Debug.WriteLine($"Finished transition");
            }

            return new Animation { animator = anim8or };
        }
    }

    public static class BuiltInAnimations
    {
        public static IEnumerator Delay(Static target, GameTime time, object argument)
        {
            var finishTime = time.TotalGameTime;
            // finicky (use Cast here or a fast numeric cast?)
            if (argument is long dooration)
                finishTime += TimeSpan.FromMilliseconds(dooration);
            else if (argument is TimeSpan duration)
                finishTime += duration;

            while (time.TotalGameTime < finishTime) yield return null;
        }

        public static IEnumerator Sequence(Static target, GameTime time, object argument)
        {
            var sequence = Data.Serializer.Cast<IEnumerable<Animation>>(argument);
            foreach (var step in sequence)
            {
                var anim = step.animator.Invoke(target, time, step.argument);
                while (anim.MoveNext()) yield return null;
            }
        }

        // rename animations to something else?
        public static IEnumerator RunCommand(Static target, GameTime time, object argument)
        {
            if (argument is string command)
                target.BubbleCommand(command);
            yield break;
        }

        // animator to disable input (somehow?, custom event handler?)
    }
}
