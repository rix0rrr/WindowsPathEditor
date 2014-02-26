using System.Windows;

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