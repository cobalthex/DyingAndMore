using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public partial class Static
    {
#if DEBUG
        public Static _FindInTreeByDebugId(uint id, bool breakOnResult = false)
        {
            var root = GetRoot();
            foreach (var elem in root.EnumerateRecursive())
            {
                if (elem.DebugId == id)
                {
                    if (breakOnResult)
                        elem.BreakOnThis();
                    return elem;
                }
            }
            return null;
        }

        private void BreakOnThis()
        {
            System.Diagnostics.Debugger.Break();
        }
#endif

        /// <summary>
        /// Get the root element in this hierarchy
        /// </summary>
        /// <returns>The root element (or this if no parent)</returns>
        public Static GetRoot()
        {
            var current = this;
            while (current.Parent != null)
                current = current.Parent;
            return current;
        }

        private Static FindNextFocus()
        {
            /* focus in the following order (13 will wrap around back to 1)

            1
                2
                3
                    4
                5
                6
            7
                8
                    9
                        10
                        11
                    12
                13

            */

            var next = this;
            while (next != null)
            {
                var current = next;
                foreach (var child in next.Children)
                {
                    if (child.CanFocus && child.IsEnabled)
                        return child;

                    if (child.Children.Count > 0)
                    {
                        next = child;
                        break;
                    }
                }

                if (current != next)
                    continue;

                while (next.Parent != null)
                {
                    var index = next.Parent.Children.IndexOf(next) + 1;
                    if (index < next.Parent.Children.Count)
                    {
                        next = next.Parent.Children[index];
                        break;
                    }
                    else
                        next = next.Parent;
                }

                if (next.CanFocus && next.IsEnabled)
                    return next;
            }

            return null;
        }

        /// <summary>
        /// Focus the next element in the tree
        /// </summary>
        /// <remarks>If this is not the focused element, finds the focused element and calls this function</remarks>
        protected Static FocusNext()
        {
            if (!HasFocus)
                return FindFocused()?.FocusNext();

            var next = FindNextFocus();
            if (next != null)
                next.HasFocus = true;
            return next;
        }

        /// <summary>
        /// Focus the previous element, using the reverse order of FocusNext()
        /// </summary>
        protected Static FocusPrevious()
        {
            if (!HasFocus)
                return FindFocused()?.FocusPrevious();

            var prev = this;
            while (true)
            {
                if (prev.Parent == null)
                {
                    if (prev.Children.Count == 0)
                        return null;

                    while (prev.Children.Count > 0)
                        prev = prev.Children[prev.Children.Count - 1];
                }
                else
                {
                    var index = prev.Parent.Children.IndexOf(prev) - 1;
                    if (index >= 0)
                    {
                        prev = prev.Parent.Children[index];

                        while (prev.Children.Count > 0)
                            prev = prev.Children[prev.Children.Count - 1];
                    }
                    else
                        prev = prev.Parent;
                }


                if (prev.CanFocus && prev.IsEnabled)
                {
                    prev.HasFocus = true;
                    return prev;
                }
            }
        }

        /// <summary>
        /// Focus the closest element in a specific direction
        /// If there are no elements in that direction, doesn't focus
        /// </summary>
        /// <param name="direction">The direction to search</param>
        /// <param name="bias">How wide to make the search</param>
        /// <returns>The focused element, or null if none</returns>
        public Static FocusGeographically(Vector2 direction, float bias = 0.25f)
        {
            direction *= new Vector2(1, -1);
            direction.Normalize();

            //todo: search parents incrementally outward

            var prox = new SortedList<float, Static>();

            var stack = new Stack<Static>();
            stack.Push(GetRoot());
            while (stack.Count > 0)
            {
                //todo: search up and down tree

                //sort by combined score of dot and dist (length?)

                var top = stack.Pop();
                if (top != this && top.CanFocus && top.IsEnabled)
                {
                    var diff = (top.VisibleContentArea.Location - VisibleContentArea.Location).ToVector2();

                    var dot = Vector2.Dot(direction, Vector2.Normalize(diff));
                    if (dot >= bias)
                    {
                        //todo: should be combination of dot and length
                        var mag = diff.LengthSquared();
                        prox[mag] = top;
                    }
                }
                foreach (var child in top.Children)
                    stack.Push(child);
            }

            if (prox.Count > 0)
            {
                prox.Values[0].HasFocus = true;
                return prox.Values[0];
            }
            return null;
        }

        /// <summary>
        /// Find the first element that can focus and focus it, starting at this element
        /// Only traverses down
        /// </summary>
        /// <returns>The focused element, null if none</returns>
        public Static FocusFirstAvailable()
        {

            //bfs
            //while (next.Count > 0)
            //{
            //    var elem = next.Dequeue();
            //    if (elem.CanFocus && elem.IsEnabled)
            //    {
            //        elem.HasFocus = true;
            //        return elem;
            //    }

            //    foreach (var child in elem.Children)
            //        next.Enqueue(child);
            //}

            var next = FindNextFocus();
            if (next != null)
                next.HasFocus = true;
            return next;
        }

        /// <summary>
        /// Find the focused element searching this and its children
        /// </summary>
        /// <returns>The focused element, or null if none</returns>
        public Static FindFocusedNoParent()
        {
            var next = new Stack<Static>();
            next.Push(this);

            while (next.Count > 0)
            {
                var elem = next.Pop();
                if (elem.HasFocus)
                    return elem;

                foreach (var child in elem.Children)
                    next.Push(child);
            }

            return null;
        }

        /// <summary>
        /// Find the element in this tree that has focus (recursively)
        /// </summary>
        /// <returns>The focused element, or null if there is none</returns>
        public Static FindFocused()
        {
            var parent = this;
            while (parent.Parent != null)
                parent = parent.Parent;

            return parent.FindFocusedNoParent();
        }

        /// <summary>
        /// Find a child element by its name (recursively)
        /// </summary>
        /// <param name="name">The name of the UI to search for</param>
        /// <returns>The first child found or null if none found with the specified name</returns>
        public Static FindChildByName(string name, bool caseSensitive = false, Type elementType = null)
        {
            var parent = this;
            while (parent.Parent != null)
                parent = parent.Parent;

            var next = new Stack<Static>();
            next.Push(parent);

            while (next.Count > 0)
            {
                var elem = next.Pop();
                if (elem.Name != null &&
                    (elementType == null || elementType.IsInstanceOfType(elem)) &&
                    elem.Name.Equals(name, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                    return elem;

                foreach (var child in elem.Children)
                    next.Push(child);
            }

            return null;
        }

        public Static FindChildAtPoint(Point point)
        {
            var process = new Stack<Static>(Children);
            var result = VisibleBounds.Contains(point) ? this : null; ;
            while (process.Count > 0)
            {
                var top = process.Pop();
                if (top.VisibleBounds.Contains(point))
                {
                    process.Clear();
                    result = top;
                    foreach (var child in top.Children)
                        process.Push(child);
                }
            }
            return result;
        }

        public T FindChildByName<T>(string name, bool caseSensitive = false) where T : Static
        {
            return (T)FindChildByName(name, caseSensitive, typeof(T));
        }

        /// <summary>
        /// Enumerate children with a matching binding source/target
        /// Searches siblings before nested children
        /// </summary>
        /// <param name="bindingSource">The name of the source binding to use, ignored if null</param>
        /// <param name="bindingTarget">The name of the target binding to use, ignored if null</param>
        /// <returns>A list of children with a matching binding</returns>
        public IEnumerable<Static> FindChildrenWithBinding(string bindingSource, string bindingTarget)
        {
            if (bindingSource == null && bindingTarget == null)
                throw new ArgumentException("bindingSource and bindingTarget cannot both be null");

            var stack = new Stack<Static>(Children);
            while (stack.Count > 0)
            {
                var top = stack.Pop();

                if (top.Bindings != null)
                {
                    foreach (var binding in top.Bindings)
                    {
                        if ((bindingSource == null || binding.Source == bindingSource) &&
                            (bindingTarget == null || binding.Target == bindingTarget))
                            yield return top;
                    }
                }

                foreach (var child in top.Children)
                    stack.Push(child);
            }
        }
    }
}
