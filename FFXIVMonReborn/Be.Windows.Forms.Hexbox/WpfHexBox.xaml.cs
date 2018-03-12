using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Be.Windows.Forms
{
    public partial class WpfHexBox : UserControl
    {
        /// <summary>
        /// Initialise HexBox
        /// </summary>
        public WpfHexBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Byte Provider Interface
        /// </summary>
        public IByteProvider ByteProvider
        {
            get { return (IByteProvider)GetValue(ByteProviderProperty); }
            set { SetValue(ByteProviderProperty, value); }
        }

        /// <summary>
        /// Byte Provider
        /// </summary>
        public static readonly DependencyProperty ByteProviderProperty =
            DependencyProperty.Register("ByteProvider", typeof(IByteProvider),
                typeof(WpfHexBox), new UIPropertyMetadata(null, ProviderChanged));

        private static void ProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WpfHexBox)d).HexBox.ByteProvider = (IByteProvider)e.NewValue;
        }

    }
}