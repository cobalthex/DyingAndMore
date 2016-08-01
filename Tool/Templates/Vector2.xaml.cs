using System.Windows;
using System.Windows.Controls;
using Xna = Microsoft.Xna.Framework;

namespace Tool.Types
{
    public partial class Vector2 : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(Xna.Vector2),
             typeof(Vector2), new FrameworkPropertyMetadata(Xna.Vector2.Zero));
        
        /// <summary>
        /// The value of this vector2 control
        /// </summary>
        public Xna.Vector2 Value
        {
            get { return (Xna.Vector2)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Vector2()
        {
            InitializeComponent();
        }
    }
}
