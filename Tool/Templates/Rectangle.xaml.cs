using System.Windows;
using System.Windows.Controls;
using Xna = Microsoft.Xna.Framework;

namespace Tool.Types
{
    public partial class Rectangle : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(Xna.Rectangle),
             typeof(Rectangle), new FrameworkPropertyMetadata(Xna.Rectangle.Empty));
        
        /// <summary>
        /// The value of this rectangle control
        /// </summary>
        public Xna.Rectangle Value
        {
            get { return (Xna.Rectangle)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Rectangle()
        {
            InitializeComponent();
        }
    }
}
