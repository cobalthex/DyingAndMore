using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Takai.Data;

namespace Takai.UI
{
    public partial class Static
    {
        public static Static GeneratePropSheet<T>(T obj = default)
        {
            return GeneratePropSheet(typeof(T), obj);
        }
        private static Static GeneratePropSheet(Type type, object obj)
        {
            if (type == typeof(Type))
                return null;

            var root = new Table {
                ColumnCount = 2,
                Margin = new Vector2(5),
                Padding = new Vector2(5),
                BackgroundColor = new Color(1, 1, 1, 0.1f),
                Style = "Frame"
            };

            if (obj == default)
            {
                //todo: allow user-specified type handlers
                if (type.IsAbstract || (!type.IsValueType && type.GetConstructor(new Type[] { }) == null))
                    return null;
                obj = Activator.CreateInstance(type);
            }
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsEnum)
            {
                var @enum = obj as Enum;
                var enumValues = Enum.GetNames(type);
                foreach (var flag in enumValues)
                {
                    var value = (Enum)Enum.Parse(type, flag);
                    if (Convert.ToUInt64(value) != 0)
                    {
                        var check = new CheckBox()
                        {
                            Name = flag,
                            Text = Util.ToSentenceCase(flag),
                            IsChecked = @enum.HasFlag(value)
                        };
                        check.On(ClickEvent, delegate (Static sender, UIEventArgs e)
                        {
                            var chkbx = (CheckBox)sender;
                            var parsed = Convert.ToUInt64(Enum.Parse(type, chkbx.Name));
                            var n = Convert.ToUInt64(@enum);

                            if (chkbx.IsChecked)
                                obj = Enum.ToObject(type, n | parsed);
                            else
                                obj = Enum.ToObject(type, n & ~parsed);

                            return UIEventResult.Handled;
                        });
                        root.AddChild(check);
                    }
                }
                return root;
            }

            //todo: item spacing (can't use list margin without nesting)

            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);

            //todo: move these into type handlers
            foreach (var member in members)
            {
                //helps avoid cyclical issues
                if (member.IsDefined(typeof(Serializer.IgnoredAttribute)))
                    continue;

                Type memberType;
                var fi = member as FieldInfo;
                var pi = member as PropertyInfo;
                object memberVal;
                if (fi != null)
                {
                    if (fi.IsInitOnly)
                        continue;
                    memberType = fi.FieldType;
                    memberVal = fi.GetValue(obj);
                }
                else if (pi != null)
                {
                    if (!pi.CanWrite)
                        continue;
                    memberType = pi.PropertyType;
                    memberVal = pi.GetValue(obj);
                }
                else
                    continue;

                if (memberType == typeof(Type))
                    continue;

                root.AddChild(new Static(Util.ToSentenceCase(member.Name))); //label

                if (typeof(ISerializeExternally).IsAssignableFrom(memberType))
                {
                    var input = new FileInput
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        //bindings
                    };
                    root.AddChild(input);
                }

                else if (memberType == typeof(bool))
                {
                    var checkbox = new CheckBox
                    {
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "IsChecked", BindingDirection.TwoWay)
                        }
                    };
                    checkbox.BindTo(obj);
                    root.AddChild(checkbox);
                }

                else if (Serializer.IsInt(memberType) ||
                    Serializer.IsFloat(memberType))
                {
                    var input = new NumericInput
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Value", BindingDirection.TwoWay)
                        }
                    };
                    input.BindTo(obj);
                    root.AddChild(input);
                }
                else if (memberType == typeof(string))
                {
                    var input = new TextInput
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Text", BindingDirection.TwoWay)
                        }
                    };
                    input.BindTo(obj);
                    root.AddChild(input);
                }
                else if (memberType == typeof(TimeSpan))
                {
                    var input = new DurationInput
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Duration", BindingDirection.TwoWay)
                        }
                    };
                    input.BindTo(obj);
                    root.AddChild(input);
                }
                else if (memberType.IsEnum)
                {
                    //todo
                    root.AddChild(new Static("(Enum)"));
                }
                else if (memberType.IsGenericType)
                {
                    var genericType = memberType.GetGenericTypeDefinition();
                    if (genericType == typeof(List<>)) //todo: make this work for any ienumerable
                    {
                        var gt = memberType.GenericTypeArguments[0];

                        var tList = typeof(ItemList<>).MakeGenericType(gt);
                        var list = Activator.CreateInstance(tList);

                        var itemUI = GeneratePropSheet(gt, default);
                        tList.GetProperty("ItemUI", BindingFlags.Public | BindingFlags.Instance)
                            .SetValue(list, itemUI);

                        var addItemUI = new List(itemUI, new Static("+") { EventCommands = { [ClickEvent] = "AddItem" }, VerticalAlignment = Alignment.Bottom })
                        {
                            Direction = Direction.Horizontal,
                            Margin = 5
                        };
                        tList.GetProperty("AddItemUI", BindingFlags.Public | BindingFlags.Instance)
                            .SetValue(list, addItemUI);

                        var st = (Static)list;
                        st.Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Items", BindingDirection.TwoWay)
                        };
                        st.HorizontalAlignment = Alignment.Stretch;
                        st.BindTo(obj);
                        root.AddChild(st);
                    }
                    //Dictionary
                    else
                        root.AddChild(new Static($"({genericType.Name}<{(string.Join(",", (object[])memberType.GenericTypeArguments))}>)"));
                }
                else if (memberType.IsArray)
                {
                    root.AddChild(new Static("(Array)"));
                }
                else
                    root.AddChild(GeneratePropSheet(memberType, memberVal) ?? new Static("?"));
            }

            return root;
        }

    }
}
