using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using System.Dynamic;

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

        public ExpandoObject Taco
        {
            get { return (ExpandoObject)GetValue(TacoProperty); }
            set { SetValue(TacoProperty, value); }
        }

        private static void SourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var src = sender as DefSheet;
            src?.BuildProperties();
        }

        private void Props_ValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            IsUnsaved = true;
            
        }

        public DefSheet()
        {
            InitializeComponent();
        }
        
        public void BuildProperties()
        {
            //Properties.Children.Clear();
            //Properties.RowDefinitions.Clear();

            if (Source == null)
                return;

            dynamic e = new ExpandoObject();
            var ed = (IDictionary<string, object>)e;

            var ty = Source.GetType();

            foreach (var field in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<Takai.Data.NonSerializedAttribute>() != null || field.GetCustomAttribute<Takai.Data.NonDesignedAttribute>() != null)
                    continue;

                if (!field.CanWrite || !field.CanRead)
                    continue;

                ed[field.Name] = field.GetValue(Source);
                //AddRow(field, field.PropertyType, Source);
            }

            foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<Takai.Data.NonSerializedAttribute>() != null || field.GetCustomAttribute<Takai.Data.NonDesignedAttribute>() != null)
                    continue;

                if (field.IsInitOnly)
                    continue;

                ed[field.Name] = field.GetValue(Source);
                //AddRow(field, field.FieldType, Source);
            }

            Taco = e;
        }

        /*
        /// <summary>
        /// Add a row
        /// </summary>
        /// <param name="Member">The property to add</param>
        /// <param name="Type">The type of the member</param>
        /// <param name="Source">The source object, for dependency properties</param>
        public void AddRow(MemberInfo Member, Type Type, object Source)
        {
            var row = Properties.RowDefinitions.Count;
            Properties.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto), MinHeight = 24 });

            var bg = new System.Windows.Shapes.Rectangle();
            bg.Fill = new SolidColorBrush(new Color { R = 0, G = 0, B = 0, A = (byte)(row % 2 == 1 ? 8 : 0) });
            bg.VerticalAlignment = VerticalAlignment.Stretch;
            bg.HorizontalAlignment = HorizontalAlignment.Stretch;
            Grid.SetRow(bg, row);
            Grid.SetColumn(bg, 0);
            Grid.SetColumnSpan(bg, 2);

            var propCol = new TypedTemplate();
            propCol.VerticalAlignment = VerticalAlignment.Center;
            propCol.Margin = new Thickness(10);
            Grid.SetRow(propCol, row);
            Grid.SetColumn(propCol, 1);

            var bind = new Binding();
            bind.Source = this.Source;
            bind.Path = new PropertyPath(Member.Name);
            bind.Mode = BindingMode.TwoWay;
            bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(propCol, TypedTemplate.ValueProperty, bind);

            var nameCol = new TextBlock() { Text = FormatName(Member.Name), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Type: " + Type.Name };
            nameCol.Margin = new Thickness(10);
            Grid.SetRow(nameCol, row);
            Grid.SetColumn(nameCol, 0);

            Properties.Children.Add(bg);
            Properties.Children.Add(nameCol);
            Properties.Children.Add(propCol);
        }*/

        /// <summary>
        /// Convert PascalCase to Sentence case
        /// </summary>
        /// <param name="Name">the name to convert</param>
        /// <returns>The converted name (or null if Name is null)</returns>
        public static string FormatName(string Name)
        {
            if (Name == null)
                return null;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            if (Name.Length > 0)
                builder.Append(char.ToUpper(Name[0]));

            for (var i = 1; i < Name.Length; i++)
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
