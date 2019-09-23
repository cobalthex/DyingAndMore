using System;
using System.Collections.Generic;
using System.Reflection;

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
            ItemUI = new Static
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
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsAbstract)
                Items.Add(type);

            foreach (var rtype in Data.Serializer.RegisteredTypes)
            {
                var rti = rtype.Value.GetTypeInfo();
                if (!rti.IsAbstract && typeInfo.IsAssignableFrom(rtype.Value))
                    Items.Add(rtype.Value);
            }
        }
    }
}
