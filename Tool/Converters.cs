using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Tool
{
    public class FileToIconConverter : IValueConverter
    {
        public static FileToIconConverter Instance = new FileToIconConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string icon = "Folder";

            var val = value as string;
            if (val.Contains("."))
            {
                var extIdx = val.IndexOf('.') + 1;
                var tkIdx = val.IndexOf(".tk");
                var path = tkIdx >= 0 ? val.Substring(extIdx, tkIdx - extIdx) : val.Substring(extIdx);
                switch (path)
                {
                    case "ent":
                        icon = "Entity";
                        break;
                    case "trig":
                        icon = "Trigger";
                        break;
                    case "wpn":
                        icon = "Gun";
                        break;
                    default:
                        icon = "Unknown";
                        break;
                }
            }

            var uri = new Uri("pack://application:,,,/Resources/" + icon + ".png");
            return new BitmapImage(uri);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException("Cannot convert back");
        }
    }

    public class TypeToIconConverter : IValueConverter
    {
        public static TypeToIconConverter Instance = new TypeToIconConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var uri = new Uri(string.Format("pack://application:,,,/Resources/{0}.png", (value is string ? value : value.GetType().Name)));
                return BitmapFrame.Create(uri);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException("Cannot convert back");
        }
    }
    public class TypeToStringConverter : IValueConverter
    {
        public static TypeToStringConverter Instance = new TypeToStringConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return value?.GetType().Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException("Cannot convert back");
        }
    }
}
