using System.Windows;
using System.Windows.Controls;
using Xna = Microsoft.Xna.Framework;

namespace Tool.Types
{
    public partial class Color : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(Xna.Color),
             typeof(Color), new FrameworkPropertyMetadata(Xna.Color.Transparent));
        
        /// <summary>
        /// The value of this color control
        /// </summary>
        public Xna.Color Value
        {
            get { return (Xna.Color)GetValue(ValueProperty); }
            set
            {
                SetValue(ValueProperty, value);
                
            }
        }

        public Color()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
