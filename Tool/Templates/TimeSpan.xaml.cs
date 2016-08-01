using System.Windows;
using System.Windows.Controls;
using Xna = Microsoft.Xna.Framework;

namespace Tool.Types
{
    public partial class TimeSpan : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
             DependencyProperty.Register("Value", typeof(System.TimeSpan),
             typeof(TimeSpan), new FrameworkPropertyMetadata(System.TimeSpan.Zero));
        
        /// <summary>
        /// The value of this time span control
        /// </summary>
        public System.TimeSpan Value
        {
            get { return (System.TimeSpan)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public TimeSpan()
        {
            InitializeComponent();
        }
    }
}
