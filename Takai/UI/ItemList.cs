using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class SelectionChangedEventArgs : UIEventArgs
    {
        public int oldIndex;
        public int newIndex;

        public SelectionChangedEventArgs(Static source, int oldIndex, int newIndex) : base(source)
        {
            this.oldIndex = oldIndex;
            this.newIndex = newIndex;
        }
    }

    /// <summary>
    /// A list of items that can optionally be selected
    /// The items can be stored in any type of <see cref="Container"/>
    /// </summary>
    /// <typeparam name="T">The type of each data-bound item</typeparam>
    public class ItemList<T> : Static
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
                    newChildren.Add(CreateItemEntry(Items[i]));
                Container.AddChildren(newChildren);
            }
        }
        private Static _itemTemplate = null;

        /// <summary>
        /// Where to render the items to
        /// </summary>
        public Static Container
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
        private Static _container = new List
        {
            Name = "ilcontainer"
            //HorizontalAlignment = Alignment.Stretch,
            //VerticalAlignment = Alignment.Stretch
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

                    var changed = new SelectionChangedEventArgs(this, _selectedIndex, value);
                    _selectedIndex = value;
                    BubbleEvent(SelectionChangedEvent, changed);
                }
            }
        }
        private int _selectedIndex = -1;

        public float ItemPadding = 10;

        /// <summary>
        /// An optional template to add a new item
        /// If null, the list is not user editable
        /// UI will be cloned from value passed in
        ///
        /// Template bindings:
        ///     item: bound item value
        ///
        /// Actions:
        ///     Add: add the item to the list and reset the template
        ///     Clear: reset the template
        /// </summary>
        public Static AddItemTemplate
        {
            get => _addItemTemplate;
            set
            {
                if (value == _addItemTemplate)
                    return;

                if (_addItemTemplate != null)
                    RemoveChild(_addItemTemplate);

                _addItemTemplate = value.CloneHierarchy();
                if (_addItemTemplate != null)
                {
                    _addItemTemplate.BindTo(newItem);
                    AddChild(_addItemTemplate);
                }
            }
        }
        private Static _addItemTemplate;
        protected T newItem = default(T);

        public ItemList()
        {
            Items.CollectionChanged += Items_CollectionChanged;
            AddChild(Container);

            CommandActions["ChangeSelection"] = delegate (Static sender, object arg)
            {
                ((ItemList<T>)sender).SelectedIndex = (int)arg;
            };
        }

        protected override void FinalizeClone()
        {
            _container = Children[Container.ChildIndex];
            base.FinalizeClone();
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
                    Container.ReplaceChild(CreateItemEntry((T)e.NewItems[i]), e.NewStartingIndex + i);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                throw new NotImplementedException("todo");
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = 0; i < e.NewItems.Count; ++i)
                    Container.InsertChild(CreateItemEntry((T)e.NewItems[i]), e.NewStartingIndex + i);
            }
        }

        /// <summary>
        /// Create a new list item
        /// </summary>
        /// <param name="value">The item value to use for binding</param>
        /// <returns>The item created</returns>
        protected Static CreateItemEntry(T value)
        {
            if (ItemTemplate == null)
                throw new NullReferenceException("ItemTemplate cannot be null");

            var item = ItemTemplate.CloneHierarchy();
            item.BindTo(value);
            item.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                sender.BubbleCommand("ChangeSelection", sender.ChildIndex);
                return UIEventResult.Continue;
            });
            return item;
        }
    }
}
