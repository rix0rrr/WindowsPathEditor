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
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PathRegistry reg = new PathRegistry();
        private readonly PathChecker checker;
        private readonly object stateLock = new object();
        private bool pathsDirty = false;

        public MainWindow()
        {
            checker = new PathChecker(reg.ExecutableExtensions);

            InitializeComponent();
            ShieldIcon = UAC.GetShieldIcon();
            searchBox.SetCompleteProvider(checker.Search);

            Read();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            checker.Dispose();
        }

        private void Read()
        {
            SetPaths(reg.SystemPath, reg.UserPath);
        }

        private void SetPaths(IEnumerable<PathEntry> systemPath, IEnumerable<PathEntry> userPath)
        {
            lock(stateLock)
            {
                SystemPath = new ObservableCollection<AnnotatedPathEntry>(systemPath.Select(AnnotatedPathEntry.FromPath));
                UserPath   = new ObservableCollection<AnnotatedPathEntry>(userPath.Select(AnnotatedPathEntry.FromPath));
    
                DirtyPaths();
    
                SystemPath.CollectionChanged += (a, b) => DirtyPaths();
                UserPath.CollectionChanged   += (a, b) => DirtyPaths();
            }
        }

        private IEnumerable<PathEntry> CurrentPath
        {
            get
            {
                lock(stateLock)
                {
                    return SystemPath.Concat(UserPath).Select(_ => _.Path);
                }
            }
        }

        /// <summary>
        /// Mark the paths as dirty and schedule a check operation
        /// </summary>
        /// <remarks>
        /// (Done like this to prevent duplicate checks scheduled in the same event handler)
        /// </remarks>
        private void DirtyPaths()
        {
            pathsDirty = true;
            Dispatcher.BeginInvoke((Action)RecheckPath);
        }

        private void RecheckPath()
        {
            if (pathsDirty)
            {
                pathsDirty = false;
                checker.Check(SystemPath.Concat(UserPath));
            }
        }

        private void Write()
        {
        }

        #region Dependency Properties
        public ObservableCollection<AnnotatedPathEntry> SystemPath
        {
            get { return (ObservableCollection<AnnotatedPathEntry>)GetValue(SystemPathProperty); }
            set { SetValue(SystemPathProperty, value); }
        }

        public static readonly DependencyProperty SystemPathProperty =
            DependencyProperty.Register("SystemPath", typeof(ObservableCollection<AnnotatedPathEntry>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollection<AnnotatedPathEntry>()));

        public ObservableCollection<AnnotatedPathEntry> UserPath
        {
            get { return (ObservableCollection<AnnotatedPathEntry>)GetValue(UserPathProperty); }
            set { SetValue(UserPathProperty, value); }
        }

        public static readonly DependencyProperty UserPathProperty =
            DependencyProperty.Register("UserPath", typeof(ObservableCollection<AnnotatedPathEntry>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollection<AnnotatedPathEntry>()));

        public BitmapSource ShieldIcon
        {
            get { return (BitmapSource)GetValue(ShieldIconProperty); }
            set { SetValue(ShieldIconProperty, value); }
        }

        public static readonly DependencyProperty ShieldIconProperty =
            DependencyProperty.Register("ShieldIcon", typeof(BitmapSource), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        /// <summary>
        /// Remove paths that don't exist or are listed multiple times
        /// </summary>
        private void Clean_Click(object sender, RoutedEventArgs e)
        {
            lock (stateLock)
            {
                var sp = SystemPath.Select(_ => _.Path);
                var up = UserPath.Select(_ => _.Path);
                var cp = sp.Concat(up);
    
                var s = sp.Where((p, index) => p.Exists && !sp.Take(index).Contains(p));
                var u = up.Where((p, index) => p.Exists && !sp.Concat(up.Take(index)).Contains(p));

                SetPaths(s, u);
            }
        }

        public Func <IDataObject, object> FileDropConverter
        {
            get
            {
                return data => {
                    var d = data as System.Windows.DataObject;
                    if (d == null || !d.ContainsFileDropList() || d.GetFileDropList().Count == 0) return null;

                    var path = d.GetFileDropList()[0];
                    if (File.Exists(path)) path = System.IO.Path.GetDirectoryName(path);

                    return new AnnotatedPathEntry(checker.EntryFromFilePath(path));
                };
            }
        }

        private AnnotatedPathEntry GetSelectedEntry(RoutedEventArgs e)
        {
            if (systemList.IsFocused || e.Source == systemList) return systemList.SelectedItem as AnnotatedPathEntry;
            if (userList.IsFocused || e.Source == userList) return userList.SelectedItem as AnnotatedPathEntry;
            return null;
        }

        private void DoExplore(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/e," + GetSelectedEntry(e).Path.ActualPath);
        }

        private void CanExplore(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GetSelectedEntry(e) != null && Directory.Exists(GetSelectedEntry(e).Path.ActualPath);
        }

        private void DoDelete(object sender, ExecutedRoutedEventArgs e)
        {
            if (systemList.IsFocused || e.Source == systemList) SystemPath.Remove(GetSelectedEntry(e));
            if (userList.IsFocused || e.Source == userList) UserPath.Remove(GetSelectedEntry(e));
        }

        private void CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GetSelectedEntry(e) != null;
        }
    }
}
