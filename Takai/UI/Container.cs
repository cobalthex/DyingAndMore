using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections;

namespace Takai.UI
{
    /// <summary>
    /// A UI element that allows for public children
    /// </summary>
    public abstract class Container : Static
    {
        public ObservableCollection<Static> Children
        {
            get => _children;
            set
            {
                if (_children == value)
                    return;

                if (_children != null)
                {
                    _children.CollectionChanged -= _children_CollectionChanged;
                }

                _children = value;
                if (_children != null)
                    _children.CollectionChanged += _children_CollectionChanged;

                Reflow();
            }
        }
        private ObservableCollection<Static> _children = new ObservableCollection<Static>();

        public Container(params Static[] children)
        {
            Children = new ObservableCollection<Static>(children);
        }

        private void _children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Reflow();
        }

        protected override int TotalChildCount => Children.Count + base.TotalChildCount;

        protected override Static GetChildAt(int index)
        {
            if (index > InternalChildren.Count)
                return Children[index - InternalChildren.Count];
            return base.GetChildAt(index);
        }

        protected override int GetChildIndex(Static child)
        {
            var index = Children.IndexOf(child);
            if (index < 0)
                return base.GetChildIndex(child);
            return index;
        }
    }
}
