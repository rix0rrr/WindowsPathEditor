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

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for DirectoryList.xaml
    /// </summary>
    public partial class DirectoryList 
    {
        public DirectoryList()
        {
            InitializeComponent();
        }

        public bool ShowIssues
        {
            get { return (bool)GetValue(ShowIssuesProperty); }
            set { SetValue(ShowIssuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowIssues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowIssuesProperty =
            DependencyProperty.Register("ShowIssues", typeof(bool), typeof(DirectoryList), new UIPropertyMetadata(true));


    }
}
