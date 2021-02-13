using System;
using System.Reflection;
using Takai.Data;

namespace Takai.UI
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)] //ComponentModel version does not allow structs
    public class DisplayNameAttribute : Attribute
    {
        public string Name { get; set; }
        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }

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
                {
                    var en = Enum.GetName(typeof(T), value);
                    for (int i = 0; i < dropdown.Items.Count; ++i)
                    {
                        if (dropdown.Items[i].name == en)
                        {
                            dropdown.SelectedIndex = i;
                            return;
                        }
                    }
                    dropdown.SelectedIndex = -1;
                }
            }
        }
        private T _value;

        protected DropdownSelect<(string name, string text)> dropdown; //dropdown for single value enums

        protected static readonly bool isFlags = typeof(T).IsDefined(typeof(FlagsAttribute));


        public EnumSelect()
        {
            Direction = Direction.Vertical;
            //Margin = new Microsoft.Xna.Framework.Vector2(5);
            Margin = 5;

            var t = typeof(T);
            var ev = Enum.GetValues(t);

            (string name, string text) GetNameText(object enumVal)
            {
                var name = Enum.GetName(t, enumVal);
                var enumMem = t.GetMember(name)[0];
                var dna = enumMem.GetCustomAttribute<DisplayNameAttribute>(false);
                if (dna != null)
                    return (name, dna.Name);

                return (name, Util.ToSentenceCase(name));
            }

            if (isFlags)
            {
                foreach (var e in ev)
                {
                    if (!Util.IsPowerOf2(Convert.ToInt64(e)))
                        continue;

                    var nameText = GetNameText(e);
                    var cb = new CheckBox
                    {
                        Name = nameText.name,
                        Text = nameText.text
                    };
                    AddChild(cb);
                }

                On(ValueChangedEvent, delegate (Static sender, UIEventArgs e)
                {
                    var cb = (CheckBox)e.Source;
                    var self = (EnumSelect<T>)sender;

                    var newv = Convert.ToInt64((T)Enum.Parse(typeof(T), cb.Name));
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
                dropdown = new DropdownSelect<(string name, string text)>()
                {
                    HorizontalAlignment = Alignment.Stretch,
                    ItemUI = new Static
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        Bindings = new System.Collections.Generic.List<Binding>
                        {
                            new Binding("Item1", "Name"),
                            new Binding("Item2", "Text")
                        }
                    }
                };
                foreach (var e in ev)
                    dropdown.Items.Add(GetNameText(e));    

                On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
                {
                    var self = (EnumSelect<T>)sender;
                    var sea = (SelectionChangedEventArgs)e;
                    if (sea.newIndex == -1)
                        Value = default;
                    else
                    {
                        var val = ((ItemList<(string name, string text)>)sea.Source).Items[sea.newIndex];
                        self.Value = (T)Enum.Parse(typeof(T), val.name);
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
                    cb.IsChecked = Value.HasFlag((Enum)Enum.Parse(typeof(T), child.Name));
            }
        }
    }
}
