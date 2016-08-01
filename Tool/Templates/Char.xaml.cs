using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Tool.Types
{
    public class CharToIntConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (char)value;
        }
    }

    public partial class Char : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(char), typeof(Char), new FrameworkPropertyMetadata((char)0));

        public static int MaxValue = char.MaxValue;

        /// <summary>
        /// The value of this char control
        /// </summary>
        public char Value
        {
            get { return (char)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Char()
        {
            InitializeComponent();
            Value = 'a';
        }
    }
}
