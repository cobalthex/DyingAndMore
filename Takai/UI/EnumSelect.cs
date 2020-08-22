using System;
using System.Reflection;
using Takai.Data;

namespace Takai.UI
{
    public class EnumSelect<T> : List where T : Enum
    {
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                if (isFlags)
                    RefreshCbValues();
                else
                    dropdown.SelectedItem = Enum.GetName(typeof(T), value);
            }
        }
        private T _value;

        protected DropdownSelect<string> dropdown; //dropdown for single value enums

        protected static readonly bool isFlags = typeof(T).IsDefined(typeof(FlagsAttribute));

        public EnumSelect()
        {
            Direction = Direction.Vertical;
            Margin = 5;

            var t = typeof(T);
            var ev = Enum.GetValues(t);

            if (isFlags)
            {
                foreach (var e in ev)
                {
                    if (!Util.IsPowerOf2(Convert.ToInt64(e)))
                        continue;

                    var cb = new CheckBox
                    {
                        Text = Enum.GetName(t, e),
                    };
                    AddChild(cb);
                }

                On(ValueChangedEvent, delegate (Static sender, UIEventArgs e)
                {
                    var cb = (CheckBox)e.Source;
                    var self = (EnumSelect<T>)sender;

                    var newv = Convert.ToInt64((T)Enum.Parse(typeof(T), cb.Text));
                    var exv = Convert.ToInt64(self.Value);
                    if (cb.IsChecked)
                        exv |= newv;
                    else
                        exv &= ~newv;

                    self.Value = (T)Enum.ToObject(typeof(T), exv);
                    return UIEventResult.Handled;
                });
            }
            else
            {
                dropdown = new DropdownSelect<string>()
                {
                    HorizontalAlignment = Alignment.Stretch,
                    ItemUI = new Static
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        Bindings = new System.Collections.Generic.List<Binding>
                        {
                            new Binding("this", "Text")
                        }
                    }
                };
                foreach (var e in ev)
                    dropdown.Items.Add(Enum.GetName(t, e));

                On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
                {
                    var self = (EnumSelect<T>)sender;
                    var sea = (SelectionChangedEventArgs)e;
                    if (sea.newIndex == -1)
                        Value = default;
                    else
                    {
                        var val = ((ItemList<string>)sea.Source).Items[sea.newIndex];
                        self.Value = (T)Enum.Parse(typeof(T), val);
                        //todo: verify works
                    }

                    return UIEventResult.Handled;
                });
                AddChild(dropdown);
                InvalidateArrange();
            }
        }

        void RefreshCbValues()
        {
            foreach (var child in Children)
            {
                if (child is CheckBox cb)
                    cb.IsChecked = Value.HasFlag((Enum)Enum.Parse(typeof(T), child.Text));
            }
        }
    }
}
