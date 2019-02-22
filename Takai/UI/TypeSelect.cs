using System;
using System.Collections.Generic;

namespace Takai.UI
{
    //A dropdown that allows picking from a list of types and constructing an instance
    public class TypeSelect : DropdownSelect<Type>
    {
        public object Instance
        {
            get
            {
                if (SelectedIndex >= 0)
                    return (_instance ?? (_instance = Activator.CreateInstance(SelectedItem)));
                else
                    return null;
            }
            set
            {
                if (value == _instance)
                    return;

                if (value == null)
                    SelectedIndex = -1;
                else
                    SelectedItem = value.GetType();

                _instance = value;
            }
        }
        private object _instance;

        public TypeSelect()
        {
            ItemTemplate = new Static
            {
                Bindings = new List<Data.Binding>
                {
                    new Data.Binding("Name", "Text")
                }
            };

            On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (TypeSelect)sender;
                self._instance = null;
                return UIEventResult.Handled;
            });
        }

        public void AddTypeTree<T>()
        {
            AddTypeTree(typeof(T));
        }

        /// <summary>
        /// Add a type and all of its subclasses to the list
        /// Ignores any abstract classes
        /// </summary>
        /// <param name="type">The type to add. Can be abstract</param>
        public void AddTypeTree(Type type)
        {
            if (!type.IsAbstract)
                Items.Add(type);

            foreach (var rtype in Data.Serializer.RegisteredTypes)
            {
                if (!rtype.Value.IsAbstract && rtype.Value.IsSubclassOf(type))
                    Items.Add(rtype.Value);
            }
        }
    }
}
