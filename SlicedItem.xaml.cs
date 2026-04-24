using System;
using System.Collections.Generic;
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

namespace ImageSlicer
{
    /// <summary>
    /// Interaction logic for SlicedItem.xaml
    /// </summary>
    public partial class SlicedItem : UserControl
    {
        public int Index { get; set; }
        public Point snapPoint { get; set; }
        public SlicedItem()
        {
            InitializeComponent();
        }
    }
}
