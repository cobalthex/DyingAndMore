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
    public class ItemList<T> : List
    {
        public const string AddItemCommand = "AddItem";
        public const string RemoveItemCommand = "RemoveItem";

        public ObservableCollection<T> Items
        {
            get => _items;
            set
            {
                if (_items == value)
                    return;

                if (value == null)
                    value = new ObservableCollection<T>();

                _items = value;
                _items.CollectionChanged += Items_CollectionChanged;
                Container.RemoveAllChildren();
                Items_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items));
            }
        }
        private ObservableCollection<T> _items;

        /// <summary>
        /// The template used to render each item.
        /// Values are bound using bindings
        /// Items are recreated whenever this is modified
        /// </summary>
        public Static ItemUI
        {
            get => _itemUI;
            set
            {
                if (value == _itemUI)
                    return;

                System.Diagnostics.Contracts.Contract.Assume(value != null, nameof(ItemUI) + " cannot be null");
                _itemUI = value;

                int focusIndex = -1;
                var focused = FindFocusedNoParent();
                if (focused != null && focused.Parent == this)
                    focusIndex = focused.ChildIndex;

                Container.RemoveAllChildren();
                var newChildren = new System.Collections.Generic.List<Static>(Items.Count);
                for (int i = 0; i < Items.Count; ++i)
                    newChildren.Add(CreateItemEntry(Items[i]));
                Container.AddChildren(newChildren);
                if (focusIndex > -1)
                    Container.Children[focusIndex].HasFocus = true;
            }
        }
        private Static _itemUI = null;

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

                var lastContainerIndex = _container.ChildIndex;
                _container = value;
                ReplaceChild(value, lastContainerIndex);
            }
        }
        private Static _container = new List
        {
            HorizontalAlignment = Alignment.Stretch,
            //VerticalAlignment = Alignment.Stretch
        };

        public bool AllowSelection { get; set; } = true;

        /// <summary>
        /// The currently selected item, can be null
        /// </summary>
        public T SelectedItem
        {
            get => SelectedIndex < 0 ? default : Items[SelectedIndex];
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
                if (value < 0 || value >= Items.Count || 
                    value >= Container.Children.Count) //shouldnt be necessary
                    value = -1;

                if (value != _selectedIndex)
                {
                    if (_selectedIndex >= 0 && _selectedIndex < Items.Count)
                    {
                        Container.Children[_selectedIndex].BorderColor = Color.Transparent;
                    }
                    if (value >= 0 && value < Items.Count)
                    {
                        Container.Children[value].BorderColor = FocusedBorderColor;
                    }

                    var changed = new SelectionChangedEventArgs(this, _selectedIndex, value);
                    _selectedIndex = value;
                    BubbleEvent(SelectionChangedEvent, changed);
                }
            }
        }
        private int _selectedIndex = -1;

        public float ItemPadding = 10;

        class NewItemContainer //class required for bindings to work
        {
#pragma warning disable CS0649
            public T item;
#pragma warning restore CS0649

            public NewItemContainer() { }

            public NewItemContainer(NewItemContainer proto)
            {
                //maybe not nullable?
                item = proto.item == null ? default : (T)Util._ShallowClone_Slow(proto.item);
            }
        }

        /// <summary>
        /// An optional UI template to add a new item
        /// If null, the list is not user editable
        /// UI will be cloned from value passed in
        ///
        /// Template bindings:
        ///     item: bound item value
        ///
        /// Actions:
        ///     Add: add the item to the list and reset the template
        ///     Remove(index): remove the specified index from the list
        ///     Clear: reset the template
        /// </summary>
        public Static AddItemUI //todo: should this go inside the container?
        {
            get => _addItemUI;
            set
            {
                if (value == _addItemUI)
                    return;

                if (_addItemUI != null)
                    RemoveChild(_addItemUI);

                _addItemUI = value.CloneHierarchy(); //todo: clone on create/add item
                if (_addItemUI != null)
                {
                    _addItemUI.BindTo(newItem);
                    AddChild(_addItemUI);
                }
            }
        }
        private Static _addItemUI;
        private NewItemContainer newItem = new NewItemContainer();

        /// <summary>
        /// The item for the AddItemTemplate
        /// </summary>
        public T AddItemTemplate
        {
            get => newItem.item;
            set
            {
                newItem.item = value;
                _addItemUI?.BindTo(newItem);
            }
        }

        public ItemList()
        {
            Items = new ObservableCollection<T>();
            AddChild(Container);
            
            CommandActions["ChangeSelection"] = delegate (Static sender, object arg)
            {
                ((ItemList<T>)sender).SelectedIndex = (int)arg;
            };

            CommandActions[AddItemCommand] = delegate (Static sender, object arg)
            {
                var il = (ItemList<T>)sender;

                if (il.newItem.item == null)
                    return;

                il.Items.Add(il.newItem.item);
                il.newItem = new NewItemContainer(il.newItem);
                il.AddItemUI.BindTo(il.newItem);
            };

            CommandActions[RemoveItemCommand] = delegate (Static sender, object arg)
            {
                var il = (ItemList<T>)sender;
                if (arg is int i)
                    il.Items.RemoveAt(i);
            };
        }

        protected override bool HandleInput(GameTime time)
        {
            //todo: change to events
            if (AllowSelection)
            {
                //todo: pageup/dn
                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Up))
                {
                    --SelectedIndex;
                    return false;
                }
                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    ++SelectedIndex;
                    return false;
                }
                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Home))
                {
                    SelectedIndex = -1;
                    return false;
                }
                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.End))
                {
                    SelectedIndex = Items.Count - 1;
                    return false;
                }
            }
            return base.HandleInput(time);
        }

        protected override void FinalizeClone()
        {
            Items = new ObservableCollection<T>(Items);
            _container = Children[Container.ChildIndex];

            if (_addItemUI != null)
            {
                newItem = new NewItemContainer(newItem);
                _addItemUI = Children[_addItemUI.ChildIndex];
                _addItemUI.BindTo(newItem);
            }

            base.FinalizeClone();
        }

        public override void BindTo(object source, System.Collections.Generic.Dictionary<string, object> customBindProps = null)
        {
            Items.Clear();
            BindToThis(source, customBindProps);
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

                //re-index children

                if (SelectedIndex >= e.OldStartingIndex)
                    SelectedIndex -= e.OldItems.Count;
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (SelectedIndex >= e.OldStartingIndex && SelectedIndex < e.NewStartingIndex + e.NewItems.Count)
                    SelectedIndex = -1;

                for (int i = 0; i < e.NewItems.Count; ++i)
                    Container.ReplaceChild(CreateItemEntry((T)e.NewItems[i], e.NewStartingIndex + i), e.NewStartingIndex + i);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                throw new NotImplementedException("todo");
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var start = e.NewStartingIndex;
                if (e.NewItems.Count > 0 && start < 0)
                    start = Items.Count;

                for (int i = 0; i < e.NewItems.Count; ++i)
                    Container.InsertChild(CreateItemEntry((T)e.NewItems[i], start + i), start + i);
            }

            var sb = new System.Text.StringBuilder();
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                    sb.Append($"+{item}\n");
            }

            _Hack_CommitChanges();
            Container.InvalidateArrange();
        }

#pragma warning disable IDE1006
        void _Hack_CommitChanges()
        {
            if (Bindings == null)
                return;

            // due to issues w/ two-way binding containers, do it manually here
            foreach (var binding in Bindings)
            {
                if (binding.Target == "Items" && binding.Direction == Data.BindingDirection.TwoWay)
                    binding.isTargetDirty = true;
            }
        }
#pragma warning restore IDE1006

        /// <summary>
        /// Create a new list item
        /// </summary>
        /// <param name="value">The item value to use for binding</param>
        /// <returns>The item created</returns>
        protected Static CreateItemEntry(T value, int index = -1)
        {
            if (ItemUI == null)
                throw new NullReferenceException(nameof(ItemUI) + " cannot be null");

            var item = ItemUI.CloneHierarchy();
            item.BindTo(value, new System.Collections.Generic.Dictionary<string, object> { [":index"] = index });
            item.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                sender.BubbleCommand("ChangeSelection", sender.ChildIndex);
                return UIEventResult.Continue;
            });
            item.CommandActions["RemoveSelf"] = delegate (Static sender, object arg)
            {
                sender.BubbleCommand(RemoveItemCommand, sender.ChildIndex);
            };
            return item;
        }
    }
}
