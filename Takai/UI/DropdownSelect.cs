using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Takai.UI
{
    public class DropdownSelect<T> : List
    {
        protected Static dropdownContainer;
        protected ScrollBox dropdown;
        public ItemList<T> list;
        protected Static preview;

        public T SelectedItem
        {
            get => list.SelectedItem;
            set
            {
                list.SelectedItem = value;
            }
        }
        public int SelectedIndex
        {
            get => list.SelectedIndex;
            set
            {
                list.SelectedIndex = value;
            }
        }

        public static Range<int> DropdownHeight { get; set; } = new Range<int>(120, 300);

        public Static ItemUI { get => list.ItemUI; set => list.ItemUI = value; }

        public System.Collections.Generic.IList<T> Items => list.Items;

        public override bool CanFocus => true;

        public bool AllowDefaultValue
        {
            get => _allowDefaultValue;
            set
            {
                if (value == _allowDefaultValue)
                    return;

                _allowDefaultValue = value;
                if (_allowDefaultValue)
                    Items.Insert(0, default);
                else
                    Items.Remove(default);
            }
        }
        bool _allowDefaultValue;

        private Graphic arrowUI;

        public DropdownSelect()
        {
            Direction = Direction.Horizontal;

            arrowUI = new Graphic
            {
                Style = "DropdownSelect.Arrow",
                HorizontalAlignment = Alignment.Right,
                VerticalAlignment = Alignment.Center,
            };
            AddChild(arrowUI);

            list = new ItemList<T>()
            {
                HorizontalAlignment = Alignment.Stretch,
            };
            list.Container.Style = "Dropdown.List";

            dropdown = new ScrollBox(list)
            {
                Style = "Dropdown"
            };

            dropdownContainer = new Static(dropdown)
            {
                Style = "Backdrop",
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };
            dropdownContainer.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                sender.RemoveFromParent();
                return UIEventResult.Handled;
            });

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (DropdownSelect<T>)sender;
                if (self.dropdownContainer.Parent != null)
                    self.CloseDropDown();
                else
                    self.OpenDropdown();

                return UIEventResult.Handled;
            });

            dropdown.On(SelectionChangedEvent, OnSelectionChanged_Dropdown);

            On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                ((DropdownSelect<T>)sender).CloseDropDown();
                return UIEventResult.Continue;
            });

            Style = "DropdownSelect";
        }

        public override void BindTo(object source, System.Collections.Generic.Dictionary<string, object> customBindProps = null)
        {
            //internal UI elements have their own bindings
            BindToThis(source, customBindProps);
        }

        protected UIEventResult OnSelectionChanged_Dropdown(Static sender, UIEventArgs e)
        {
            //clones must bind directly to this

            //todo: arrow keying through dropdown entries sometimes leaves preview blank
            // (fills in once dropdown is closed)
            var sea = (SelectionChangedEventArgs)e;

            var previewChildIndex = preview?.ChildIndex ?? -1;
            if (sea.newIndex >= 0)
            {
                preview = list.Container.Children[sea.newIndex].CloneHierarchy();
                preview.HorizontalAlignment = Alignment.Left; // this shouldn't be necessary
                preview.BindTo(list.Items[sea.newIndex]);

                if (previewChildIndex < 0)
                    InsertChild(preview, 0);
                else
                    ReplaceChild(preview, previewChildIndex);
            }
            else if (preview != null)
                preview.BindTo(null);

            BubbleEvent(this, SelectionChangedEvent, e);
            return UIEventResult.Handled; //the dropdown is not part of the main tree
        }

        protected override void FinalizeClone()
        {
            dropdownContainer = dropdownContainer.CloneHierarchy();
            dropdown = (ScrollBox)dropdownContainer.Children[0];
            list = (ItemList<T>)System.Linq.Enumerable.First(dropdown.EnumerableChildren);
            arrowUI = (Graphic)Children[arrowUI.ChildIndex];

            var previewChildIndex = preview?.ChildIndex ?? -1;
            if (previewChildIndex >= 0)
            {
                preview = list.Container.Children[list.SelectedIndex];
                preview.BindTo(list.SelectedItem);
                InvalidateArrange();
            }
            else
                preview = null;

            dropdown.Off(SelectionChangedEvent);
            dropdown.On(SelectionChangedEvent, OnSelectionChanged_Dropdown);

            base.FinalizeClone();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            base.MeasureOverride(availableSize); //has to pre-calc stretches
            dropdownContainer.Measure(InfiniteSize);
            return new Vector2(list.MeasuredSize.X == 0 ? 200 : list.MeasuredSize.X, 30); //todo
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            if (dropdownContainer.Parent != null)
            {
                var root = GetRoot();

                var dsz = new Point(
                    (int)availableSize.X,
                    (int)dropdownContainer.MeasuredSize.Y
                );
                dsz.Y = MathHelper.Clamp(
                    Math.Min(dsz.Y, DropdownHeight.max),
                    DropdownHeight.min,
                    root.OffsetContentArea.Height - OffsetContentArea.Bottom
                );

                var dpos = new Point(OffsetContentArea.Left - (int)Padding.X, OffsetContentArea.Bottom);

                //keep on-screen
                dpos.X = MathHelper.Clamp(dpos.X, 0, root.OffsetContentArea.Width - dsz.X);
                dpos.Y = MathHelper.Clamp(dpos.Y, 0, root.OffsetContentArea.Height - dsz.Y);

                dropdownContainer.Arrange(root.OffsetContentArea);
                //dropdown.Measure(dsz.ToVector2()); //todo: this is not working
                dropdown.Arrange(new Rectangle(dpos.X, dpos.Y, dsz.X, dsz.Y));
            }
            base.ArrangeOverride(availableSize);
        }

        public virtual void OpenDropdown()
        {
            if (Items.Count < 1)
                return;

            var root = GetRoot();
            root.AddChild(dropdownContainer);
            InvalidateArrange();
            // arrange now?
        }

        public virtual void CloseDropDown()
        {
            dropdownContainer.RemoveFromParent();
        }

        protected override bool HandleInput(GameTime time)
        {
            if (dropdownContainer.Parent != null &&
                (Input.InputState.IsPress(Keys.Escape) ||
                Input.InputState.IsPress(Keys.Space) ||
                Input.InputState.IsPress(Keys.Enter)))
                CloseDropDown();

            else if (Input.InputState.IsPress(Keys.Up))
                --SelectedIndex;
            else if (Input.InputState.IsPress(Keys.Down))
                ++SelectedIndex;
            else if (Input.InputState.IsPress(Keys.Home))
                SelectedIndex = -1;
            else if (Input.InputState.IsPress(Keys.End))
                SelectedIndex = Items.Count - 1;
            else
                return base.HandleInput(time);

            return false;
        }

        public override void ApplyStyleRules(System.Collections.Generic.Dictionary<string, object> styleRules)
        {
            base.ApplyStyleRules(styleRules);
            if (arrowUI != null)
                arrowUI.Sprite = GetStyleRule(styleRules, "ArrowSprite", arrowUI.Sprite);
        }
    }
}
