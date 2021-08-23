using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Takai.UI
{
    public partial class Static
    {
        // used to track the deepest hovered element to not show every element under the cursor as highlighted
        private static Static HoveredElement;

        /// <summary>
        /// Update this element and all of its children
        /// </summary>
        /// <param name="time">Game time</param>
        public virtual void Update(GameTime time)
        {
#if DEBUG
            boop.Restart();
#endif
            PerformReflows();

            if (!IsEnabled)
                return;

            /* update in the following order: H G F E D C B A
            A
                B
                    C
                    D
                E
                    F
                        G
                    H
            */

            //find deepest darkest child
            var toUpdate = this;
            for (int i = toUpdate.Children.Count - 1; i >= 0; --i)
            {
                if (!toUpdate.Children[i].IsEnabled)
                    continue;

                toUpdate = toUpdate.Children[i];
                i = toUpdate.Children.Count;
            }

            Static lastHover = HoveredElement;
            HoveredElement = null;

            bool handleInput = Runtime.HasFocus;
            while (true)
            {
                if (handleInput)
                    handleInput = toUpdate.HandleInput(time) && !toUpdate.IsModal;

                toUpdate.UpdateSelf(time);

                //stop at this element
                if (toUpdate.Parent == null || toUpdate == this)
                    break;

                //iterate through previous children of current level
                var i = toUpdate.Parent._children.IndexOf(toUpdate) - 1;
                for (; i >= 0; --i)
                {
                    toUpdate = toUpdate.Parent.Children[i];
                    if (!toUpdate.IsEnabled)
                        continue;

                    //find deepest child
                    for (int j = toUpdate.Children.Count - 1; j >= 0; --j)
                    {
                        if (!toUpdate.Children[j].IsEnabled)
                            continue;

                        toUpdate = toUpdate.Children[j];
                        j = toUpdate.Children.Count;
                    }
                    break;
                }
                if (i < 0) //todo: does this skip the first child?
                    toUpdate = toUpdate.Parent;
            }

            if (lastHover != HoveredElement)
            {
                if (lastHover != null)
                    lastHover.InvalidateStyle();
                if (HoveredElement != null)
                    HoveredElement.InvalidateStyle();
            }

#if DEBUG
            lastUpdateDuration = boop.Elapsed;
#endif
        }

        /// <summary>
        /// Update this UI's state here. Input should be handled in <see cref="HandleInput"/>
        /// Bindings should be applied here
        /// </summary>
        /// <param name="time">game time</param>
        protected virtual void UpdateSelf(GameTime time)
        {
            if (Bindings != null)
            {
                foreach (var binding in Bindings)
                    binding.Update();
            }

            //child bind scopes (hacky)
            if (bindScopeGetset.get != null)
            {
                var newVal = bindScopeGetset.get();
                var newHash = (newVal ?? 0).GetHashCode();
                if (newHash != bindScopeGetset.cachedHash)
                {
                    bindScopeGetset.cachedValue = newVal;
                    bindScopeGetset.cachedHash = newHash;

                    foreach (var child in Children)
                        child.BindTo(newVal);
                }
            }

            if (queuedAnimations != null)
            {
                while (queuedAnimations.Count > 0)
                {
                    if (activeAnimations == null)
                        activeAnimations = new List<System.Collections.IEnumerator>();

                    var animation = queuedAnimations.Dequeue();
                    activeAnimations.Add(animation.animator.Invoke(this, time, animation.argument));
                }
            }
            if (activeAnimations != null)
            {
                for (int i = 0; i < activeAnimations.Count; ++i)
                {
                    if (!activeAnimations[i].MoveNext())
                    {
                        activeAnimations[i] = activeAnimations[activeAnimations.Count - 1];
                        activeAnimations.RemoveAt(activeAnimations.Count - 1);
                    }
                }
            }
        }
    }
}
