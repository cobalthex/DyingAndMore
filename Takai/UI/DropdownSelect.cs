﻿using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class SelectionList<T> : List
    {
        public ObservableCollection<T> Items { get; private set; } = new ObservableCollection<T>();

        /// <summary>
        /// The currently selected item, can be null
        /// </summary>
        public T SelectedItem
        {
            get => Items[SelectedIndex];
            set => SelectedIndex = Items.IndexOf(value);
        }

        /// <summary>
        /// The index in the Items list of the selected item
        /// </summary>
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value < 0 || value >= Items.Count)
                    value = -1;

                if (value != selectedIndex)
                {
                    if (selectedIndex >= 0 && selectedIndex < Items.Count)
                    {
                        Children[selectedIndex].BorderColor = Color.Transparent;
                    }
                    if (value >= 0 && value < Items.Count)
                    {
                        Children[value].BorderColor = Static.FocusedBorderColor;
                    }
                    selectedIndex = value;
                    OnSelectionChanged(System.EventArgs.Empty);
                    SelectionChanged?.Invoke(this, System.EventArgs.Empty);
                }
            }
        }
        private int selectedIndex = -1;

        public System.EventHandler SelectionChanged { get; set; }
        protected virtual void OnSelectionChanged(System.EventArgs e) { }

        public SelectionList()
        {
            Items.CollectionChanged += Items_CollectionChanged;
        }

        protected void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                RemoveAllChildren();
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                for (int i = 0; i < e.OldItems.Count; ++i)
                    RemoveChildAt(e.OldStartingIndex + i);
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                for (int i = 0; i < e.NewItems.Count; ++i)
                   ReplaceChild(CreateItem((T)e.NewItems[i]), e.NewStartingIndex + i);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                ; //todo
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = 0; i < e.NewItems.Count; ++i)
                    InsertChild(CreateItem((T)e.NewItems[i]), e.NewStartingIndex + i);
            }
        }

        protected Static CreateItem(T value)
        {
            var item = new Static()
            {
                Text = value.ToString(),
                Font = Font,
                Color = Color,
                HorizontalAlignment = Alignment.Stretch
            };
            item.AutoSize();
            item.Click += delegate (object sender, ClickEventArgs e)
            {
                var which = (Static)sender;
                SelectedIndex = Children.IndexOf(which);
            };
            return item;
        }
    }

    public class DropdownSelect<T> : Static
    {
        protected ScrollBox dropdown = new ScrollBox();
        protected SelectionList<T> list = new SelectionList<T>();
        bool isDropdownOpen = false;

        public ObservableCollection<T> Items => list.Items;

        public T SelectedItem
        {
            get => list.SelectedItem;
            set => list.SelectedItem = value;
        }
        public int SelectedIndex
        {
            get => list.SelectedIndex;
            set => list.SelectedIndex = value;
        }

        public DropdownSelect()
        {
            dropdown.BorderColor = BorderColor = Color.White;
            list.HorizontalAlignment = Alignment.Stretch;

            dropdown.AddChild(list);
            list.SelectionChanged += delegate
            {
                Text = SelectedItem?.ToString();
                isDropdownOpen = false;
            };
        }

        public override bool CanFocus => true;

        public void OpenDropdown()
        {
            isDropdownOpen = true;

            list.AutoSize();
            dropdown.Size = new Vector2(Size.X, MathHelper.Min(list.Size.Y, 200));

            var end = new Vector2(VisibleBounds.Right, VisibleBounds.Bottom) + dropdown.Size;
            if (end.X > Runtime.GraphicsDevice.Viewport.Width ||
                end.Y > Runtime.GraphicsDevice.Viewport.Height)
                dropdown.Position = VisibleBounds.Location.ToVector2() - new Vector2(0, dropdown.Size.Y);
            else
                dropdown.Position = VisibleBounds.Location.ToVector2() + new Vector2(0, Size.Y); //todo: smarter placement
        }

        protected override void OnClick(ClickEventArgs e)
        {
            if (isDropdownOpen)
                isDropdownOpen = false;
            else
                OpenDropdown();
        }

        protected override void UpdateSelf(GameTime time)
        {
            if (isDropdownOpen)
                dropdown.Update(time);
            base.UpdateSelf(time);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (isDropdownOpen)
            {
                if (Input.InputState.IsPress(Input.MouseButtons.Left) && !dropdown.VirtualBounds.Contains(Input.InputState.MousePoint))
                    isDropdownOpen = false;
                return false;
            }
            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            DrawText(spriteBatch, new Point(2, 2 + (int)(Size.Y - textSize.Y - 4) / 2));
            if (isDropdownOpen)
                dropdown.Draw(spriteBatch);
        }
    }
}