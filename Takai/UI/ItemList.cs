using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class SelectionChangedEventArgs : EventArgs
    {
        public int oldIndex;
        public int newIndex;
    }

    /// <summary>
    /// A list of items that can optionally be selected
    /// The items can be stored in any type of <see cref="Container"/>
    /// </summary>
    /// <typeparam name="T">The type of each data-bound item</typeparam>
    public class ItemList<T> : Static //todo: inherit from ScrollBox?
    {
        public ObservableCollection<T> Items { get; set; } = new ObservableCollection<T>();

        /// <summary>
        /// The template used to render each item.
        /// Values are bound using bindings
        /// Items are recreated whenever this is modified
        /// </summary>
        public Static ItemTemplate
        {
            get => _itemTemplate;
            set
            {
                if (value == _itemTemplate)
                    return;

                System.Diagnostics.Contracts.Contract.Assume(value != null, nameof(ItemTemplate) + " cannot be null");
                _itemTemplate = value;

                int focusIndex = -1;
                var focused = FindFocusedNoParent();
                if (focused != null && focused.Parent == this)
                    focusIndex = focused.ChildIndex;

                Container.RemoveAllChildren();
                var newChildren = new System.Collections.Generic.List<Static>(Items.Count);
                for (int i = 0; i < Items.Count; ++i)
                    newChildren.Add(CreateItem(Items[i]));
                Container.AddChildren(newChildren);
            }
        }
        private Static _itemTemplate = null;

        /// <summary>
        /// Where to render the items to
        /// </summary>
        public Container Container
        {
            get => _container;
            set
            {
                if (Container == value)
                    return;

                System.Diagnostics.Contracts.Contract.Assume(value != null, nameof(Container) + " cannot be null");

                if (_container != null)
                    _container.MoveAllChildrenTo(value);

                _container = value;
                ReplaceAllChildren(_container);
            }
        }
        private Container _container = new List
        {
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch
        };

        public bool AllowSelection { get; set; } = true;

        /// <summary>
        /// The currently selected item, can be null
        /// </summary>
        public T SelectedItem
        {
            get => SelectedIndex < 0 ? default(T) : Items[SelectedIndex];
            set => SelectedIndex = Items.IndexOf(value);
        }

        /// <summary>
        /// The index in the Items list of the selected item
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 || value >= Items.Count)
                    value = -1;

                if (value != _selectedIndex)
                {
                    if (_selectedIndex >= 0 && _selectedIndex < Items.Count)
                    {
                        Container.Children[_selectedIndex].BorderColor = Color.Transparent;
                    }
                    if (value >= 0 && value < Items.Count)
                    {
                        Container.Children[value].BorderColor = Static.FocusedBorderColor;
                    }

                    var changed = new SelectionChangedEventArgs
                    {
                        oldIndex = _selectedIndex,
                        newIndex = value
                    };

                    _selectedIndex = value;
                    OnSelectionChanged(changed);
                    SelectionChanged?.Invoke(this, changed);
                }
            }
        }
        private int _selectedIndex = -1;

        public float ItemPadding = 10;

        public EventHandler<SelectionChangedEventArgs> SelectionChanged { get; set; }
        protected virtual void OnSelectionChanged(SelectionChangedEventArgs e) { }

        public ItemList()
        {
            Items.CollectionChanged += Items_CollectionChanged;
            AddChild(Container);
        }

        protected void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Container.RemoveAllChildren();
                SelectedIndex = -1;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                for (int i = 0; i < e.OldItems.Count; ++i)
                    Container.RemoveChildAt(e.OldStartingIndex + i);

                if (SelectedIndex >= e.OldStartingIndex)
                    SelectedIndex -= e.OldItems.Count;
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (SelectedIndex >= e.OldStartingIndex && SelectedIndex < e.NewStartingIndex + e.NewItems.Count)
                    SelectedIndex = -1;

                for (int i = 0; i < e.NewItems.Count; ++i)
                    Container.ReplaceChild(CreateItem((T)e.NewItems[i]), e.NewStartingIndex + i);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                throw new NotImplementedException("todo");
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = 0; i < e.NewItems.Count; ++i)
                    Container.InsertChild(CreateItem((T)e.NewItems[i]), e.NewStartingIndex + i);
            }
        }

        /// <summary>
        /// Create a new list item
        /// </summary>
        /// <param name="value">The item value to use for binding</param>
        /// <returns>The item created</returns>
        protected Static CreateItem(T value)
        {
            if (ItemTemplate == null)
                throw new NullReferenceException("ItemTemplate cannot be null");

            var item = ItemTemplate.Clone();
            item.BindTo(value);
            item.Click += delegate (object sender, ClickEventArgs e)
            {
                var which = (Static)sender;
                SelectedIndex = which.ChildIndex;
            };
            return item;
        }
    }
}
