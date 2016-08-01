using System.Windows;
using System.Windows.Controls;
using Xna = Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Specialized;

namespace Tool.Types
{
    //public class EnumExpansion : DependencyObject, IList, INotifyCollectionChanged
    //{
    //    public object EnumValue
    //    {
    //        get;set;
    //    }
    //}

    public partial class Flags : UserControl
    {
        //public static readonly DependencyProperty ValueProperty =
        //     DependencyProperty.Register("Value", typeof(Xna.Rectangle),
        //     typeof(Rectangle), new FrameworkPropertyMetadata(Xna.Rectangle.Empty));

        //enum type
        //enum object
        //type converter

        System.Enum Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;

                var ty = value.GetType();
                var ut = ty.GetEnumUnderlyingType();

                var n = 0;
                foreach (var i in ty.GetEnumValues())
                {
                    n++;
                    if (n == 1)
                        continue;

                    var uv = System.Convert.ChangeType(i, ut);

                    var checkbox = new CheckBox();
                    checkbox.VerticalAlignment = VerticalAlignment.Center;
                    checkbox.Margin = new Thickness(2);
                    checkbox.Content = DefSheet.FormatName(i.ToString());
                    checkbox.ToolTip = "Value: " + uv.ToString();
                    checkbox.IsChecked = value.HasFlag(i as System.Enum);

                    checkbox.Checked += (object sender, RoutedEventArgs e) =>
                    {
                    };

                    Properties.Children.Add(checkbox);
                }
            }
        }
        System.Enum value;
        
        [System.Flags]
        enum Test
        {
            None = 0,
            ABC = 1,
            B = 2,
            C = 4,
            D = 8,
        }

        public Flags()
        {
            InitializeComponent();

            Test t = Test.C | Test.D;
            Value = t;
        }
    }
}
