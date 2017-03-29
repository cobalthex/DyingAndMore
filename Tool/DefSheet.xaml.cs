using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using System.Dynamic;
using System.ComponentModel;
using System.Windows.Data;

namespace Tool
{
    public partial class DefSheet : ScrollViewer
    {
        public static readonly DependencyProperty FileProperty = DependencyProperty.Register("File", typeof(string), typeof(DefSheet), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(object), typeof(DefSheet), new PropertyMetadata(null, SourceChanged));
        public static readonly DependencyProperty IsUnsavedProperty = DependencyProperty.Register("IsUnsaved", typeof(bool), typeof(DefSheet), new PropertyMetadata(false));

        public static readonly DependencyProperty TacoProperty = DependencyProperty.Register("Taco", typeof(ExpandoObject), typeof(DefSheet), new PropertyMetadata(null));

        public string File
        {
            get { return (string)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }

        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public bool IsUnsaved
        {
            get { return (bool)GetValue(IsUnsavedProperty); }
            set { SetValue(IsUnsavedProperty, value); }
        }
        
        private static void SourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void Props_ValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            IsUnsaved = true;
            
        }

        public DefSheet()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Convert PascalCase to Sentence case
        /// </summary>
        /// <param name="Name">the name to convert</param>
        /// <returns>The converted name (or null if Name is null)</returns>
        public static string FormatName(string Name)
        {
            if (Name == null)
                return "";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            if (Name.Length > 0)
                builder.Append(char.ToUpper(Name[0]));

            for (var i = 1; i < Name.Length; ++i)
            {
                if (char.IsUpper(Name[i]))
                {
                    builder.Append(' ');
                    builder.Append(char.ToLower(Name[i]));
                }
                else
                    builder.Append(Name[i]);
            }
            return builder.ToString();
        }
    }
}
