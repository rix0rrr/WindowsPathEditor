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
using System.Collections.ObjectModel;

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PathRegistry reg = new PathRegistry();
        private readonly ConflictChecker checker = new ConflictChecker();

        public MainWindow()
        {
            InitializeComponent();
            ShieldIcon = UAC.GetShieldIcon();

            Read();
        }

        private void Read()
        {
            SystemPath = new ObservableCollection<PathEntry>(reg.SystemPath);
            UserPath   = new ObservableCollection<PathEntry>(reg.UserPath);

            SystemPath.Concat(UserPath).Each(x => checker.Check(x));
        }

        private void Write()
        {
        }

        #region Dependency Properties
        public ObservableCollection<PathEntry> SystemPath
        {
            get { return (ObservableCollection<PathEntry>)GetValue(SystemPathProperty); }
            set { SetValue(SystemPathProperty, value); }
        }

        public static readonly DependencyProperty SystemPathProperty =
            DependencyProperty.Register("SystemPath", typeof(ObservableCollection<PathEntry>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollection<PathEntry>()));

        public ObservableCollection<PathEntry> UserPath
        {
            get { return (ObservableCollection<PathEntry>)GetValue(UserPathProperty); }
            set { SetValue(UserPathProperty, value); }
        }

        public static readonly DependencyProperty UserPathProperty =
            DependencyProperty.Register("UserPath", typeof(ObservableCollection<PathEntry>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollection<PathEntry>()));

        public BitmapSource ShieldIcon
        {
            get { return (BitmapSource)GetValue(ShieldIconProperty); }
            set { SetValue(ShieldIconProperty, value); }
        }

        public static readonly DependencyProperty ShieldIconProperty =
            DependencyProperty.Register("ShieldIcon", typeof(BitmapSource), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

    }
}
