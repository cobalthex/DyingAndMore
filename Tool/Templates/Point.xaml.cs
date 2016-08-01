using System.Windows;
using System.Windows.Controls;
using Xna = Microsoft.Xna.Framework;

namespace Tool.Types
{
    public partial class Point : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(Xna.Point),
             typeof(Point), new FrameworkPropertyMetadata(Xna.Point.Zero));
        
        /// <summary>
        /// The value of this vector2 control
        /// </summary>
        public Xna.Point Value
        {
            get { return (Xna.Point)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Point()
        {
            InitializeComponent();
        }
    }
}
