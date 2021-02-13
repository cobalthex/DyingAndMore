using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Takai.UI
{
    public partial class Static
    {
        /// <summary>
        /// Who owns/contains this element
        /// </summary>
        [Data.Serializer.Ignored]
        public Static Parent
        {
            get => _parent;
            protected set
            {
                if (_parent == value)
                    return;

                SetParentNoReflow(value);
                Parent?.OnChildRemeasure(this); //todo: evaluate
            }
        }
        [Data.Serializer.Ignored]
        private Static _parent = null;

        /// <summary>
        /// A readonly collection of all of the children in this element (including disabled children)
        /// </summary>
        [Data.CustomDeserialize(typeof(Static), "DeserializeChildren")]
        public ReadOnlyCollection<Static> Children { get; private set; } //todo: maybe observable
        private List<Static> _children = new List<Static>();

        private void SetParentNoReflow(Static newParent) //todo: re-evaluate necessity
        {
            var oldParent = _parent;
            _parent = newParent;
#if DEBUG
            //todo: make this on-demand?
            foreach (var child in EnumerateRecursive())
            {
                child.DebugTreePath = $"/{(child.GetType().Name)}({child.DebugId})";
                if (child.Parent != null)
                    child.DebugTreePath = child.Parent.DebugTreePath + child.DebugTreePath;
            }
#endif

            OnParentChanged(oldParent);
        }

        /// <summary>
        /// Remove this element from its parent. If parent is null, does nothing
        /// </summary>
        /// <returns>True if the element was removed from its parent or false if parent was null</returns>
        public bool RemoveFromParent()
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The index of this child in its parents, -1 if no parent
        /// </summary>
        public int ChildIndex => Parent?.Children.IndexOf(this) ?? -1;

        /// <summary>
        /// Get the index of a child to this element
        /// </summary>
        /// <param name="child">The child to search</param>
        /// <returns>-1 if the element is not a child or null</returns>
        public int IndexOf(Static child)
        {
            return Children.IndexOf(child);
        }

        /// <summary>
        /// Insert the child into the children without reflowing
        /// </summary>
        /// <param name="child">The child element to add</param>
        /// <param name="index">The insert to add at. Out of bounds are added to the end</param>
        /// <param name="ignoreFocus">ignore <see cref="HasFocus"/></param>
        /// <returns>True if the child as added, false otherwise</returns>
        protected virtual bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            //Diagnostics.Debug.WriteLine($"Inserting child ID:{child.DebugId} @ {index} into ID:{DebugId}");
            //todo: maybe have a forward setting (forward all additions to specified child)

            if (child == null || child.Parent == this)
                return false;

            if (child.Parent != null)
                child.RemoveFromParent();

            child.SetParentNoReflow(this);

            if (index < 0 || index >= _children.Count)
                _children.Add(child);
            else
                _children.Insert(index, child);

            if (child.HasFocus && !ignoreFocus)
                child.HasFocus = true;

            if (reflow) //force rebase now (prevents elements from popping in) ?
                OnChildRemeasure(child);

            return true;
        }

        /// <summary>
        /// Swap a child, can be set to null
        /// </summary>
        /// <param name="child">The new child to replace the old with</param>
        /// <param name="index">The index to swap. Must be valid child index</param>
        /// <param name="reflow">Reflow after swapping</param>
        /// <returns>The old element that was swapped out, or null if the index is out of bounds</returns>
        protected virtual Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            if (index < 0 || index >= Children.Count)
                throw new System.ArgumentOutOfRangeException(nameof(index) + " must be between 0 and the number of children");

            var old = Children[index];
            if (old.Parent == this)
                old.SetParentNoReflow(null);

            _children[index] = child;
            if (child != null)
            {
                child.SetParentNoReflow(this);
                if (child.HasFocus && !ignoreFocus)
                    child.HasFocus = true;
            }

            if (reflow)
                OnChildRemeasure(this);

            return old;
        }

        /// <summary>
        /// Remove a child element at the specified index
        /// </summary>
        /// <param name="index">The index to remove. Must be valid child index</param>
        /// <param name="reflow">Reflow after removing</param>
        /// <returns>The element that was removed, or null if the index is out of bounds</returns>
        protected virtual Static InternalRemoveChild(int index, bool reflow = true)
        {
            if (index < 0 || index >= Children.Count)
                throw new System.ArgumentOutOfRangeException(nameof(index) + " must be between 0 and the number of children");

            var old = Children[index];
            if (old.Parent == this)
                old.SetParentNoReflow(null);

            _children.RemoveAt(index);

            if (reflow)
                OnChildRemeasure(this);

            return old;
        }

        public Static AddChild(Static child)
        {
            InternalInsertChild(child);
            return child;
        }

        /// <summary>
        /// Replace the child at a specific index
        /// </summary>
        /// <param name="child">the child to replace with</param>
        /// <param name="index">the index of the child to replace. Throws if out of range</param>
        /// <returns>The staticadded</returns>
        public Static ReplaceChild(Static child, int index)
        {
            InternalSwapChild(child, index);
            return child;
        }

        public Static InsertChild(Static child, int index = 0)
        {
            InternalInsertChild(child, index);
            return child;
        }

        public void AddChildren(params Static[] children)
        {
            AddChildren((IEnumerable<Static>)children);
        }

        public void AddChildren(IEnumerable<Static> children)
        {
            //todo: add disable reflow and then switch this to use InsertChild
            int count = Children.Count;

            //todo: set parent normally?
            foreach (var child in children)
                child?.SetParentNoReflow(null);

            Static lastFocus = null;
            foreach (var child in children)
            {
                if (InternalInsertChild(child, -1, false, true) && child.HasFocus)
                    lastFocus = child;
            }

            if (lastFocus != null)
                lastFocus.HasFocus = true;

            if (Children.Count != count)
                InvalidateMeasure();
        }

        /// <summary>
        /// Remove an element from this element. Does not search children
        /// </summary>
        /// <param name="child"></param>
        public Static RemoveChild(Static child)
        {
            var index = IndexOf(child);
            InternalRemoveChild(index);
            return child;
        }

        public Static RemoveChildAt(int index)
        {
            return InternalRemoveChild(index);
        }

        public void RemoveAllChildren()
        {
            //todo: this may break things like Accordians
            var count = _children.Count;
            if (count == 0)
                return;

            // todo: this is broken with types with hidden children
            for (int i = 0; i < count; ++i)
                InternalSwapChild(null, i, false);
            _children.Clear();
            OnChildRemeasure(this);
        }

        /// <summary>
        /// Move all children from this element to another
        /// </summary>
        /// <param name="target">The target element to move them to</param>
        public void MoveAllChildrenTo(Static target)
        {
            //todo: use internal?
            target.AddChildren(_children);
            _children.Clear();
            InvalidateMeasure();
        }

        public void ReplaceAllChildren(params Static[] newChildren)
        {
            RemoveAllChildren();
            AddChildren(newChildren);
        }
        public void ReplaceAllChildren(IEnumerable<Static> newChildren)
        {
            RemoveAllChildren();
            AddChildren(newChildren);
        }

        /// <summary>
        /// Enumerate through all children and their descendents recursively (including this)
        /// This can be overriden by
        /// </summary>
        /// <returns>An enumerator to all elements</returns>
        public IEnumerable<Static> EnumerateRecursive()
        {
            Stack<Static> enumeration = new Stack<Static>();
            enumeration.Push(this);
            while (enumeration.Count > 0)
            {
                var top = enumeration.Pop();
                yield return top;

                foreach (var child in top.Children)
                {
                    if (child.IsEnabled)
                        enumeration.Push(child);
                }
            }
        }
    }
}
