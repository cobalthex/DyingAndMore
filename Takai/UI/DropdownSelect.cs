﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    //todo: modernize

    public class DropdownSelect<T> : Static
    {
        protected ScrollBox dropdown = new ScrollBox();
        protected ItemList<T> list = new ItemList<T>();
        bool isDropdownOpen = false;

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

        public System.Collections.Generic.ICollection<T> Items => list.Items;

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

            dropdown.Size = new Vector2(Size.X, System.Math.Min(list.Size.Y, 200));

            var end = new Vector2(VisibleContentArea.Right, VisibleContentArea.Bottom) + dropdown.Size;
            if (end.X > Runtime.GraphicsDevice.Viewport.Width ||
                end.Y > Runtime.GraphicsDevice.Viewport.Height)
                dropdown.Position = VisibleContentArea.Location.ToVector2() - new Vector2(0, dropdown.Size.Y);
            else
                dropdown.Position = VisibleContentArea.Location.ToVector2() + new Vector2(0, Size.Y); //todo: smarter placement
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
                if (Input.InputState.IsPress(Input.MouseButtons.Left))
                    isDropdownOpen = false;
                return false;
            }
            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (isDropdownOpen)
                dropdown.Draw(spriteBatch);
        }
    }
}
