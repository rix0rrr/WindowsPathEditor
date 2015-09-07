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
using System.Windows.Interop;
using System.ComponentModel;
using System.Threading.Tasks;

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static RoutedCommand CleanUp = new RoutedCommand();

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

            var args = Environment.GetCommandLineArgs();
            if (args.Count() > 1)
            {
                WriteChangesFromCommandLine(args.Skip(1));
                Close();
            }
            else
                Read();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            checker.Dispose();
        }

        /// <summary>
        /// Write the changes passed on the command-line (used for writing with UAC elevation)
        /// </summary>
        private void WriteChangesFromCommandLine(IEnumerable<string> args)
        {
            for (int i = 0; i < args.Count(); i++)
            {
                if (args.ElementAt(i).ToLower() == "/system") {
                    reg.SystemPath = ParseCommandLinePath(args.ElementAt(i + 1));
                    i++;
                }
                if (args.ElementAt(i).ToLower() == "/user") {
                    reg.UserPath = ParseCommandLinePath(args.ElementAt(i + 1));
                    i++;
                }
            }
        }

        private void Read()
        {
            SetPaths(reg.SystemPath, reg.UserPath);
        }

        private void SetPaths(IEnumerable<PathEntry> systemPath, IEnumerable<PathEntry> userPath)
        {
            lock(stateLock)
            {
                SystemPath = new ObservableCollectionEx<AnnotatedPathEntry>(systemPath.Select(AnnotatedPathEntry.FromPath));
                UserPath   = new ObservableCollectionEx<AnnotatedPathEntry>(userPath.Select(AnnotatedPathEntry.FromPath));
    
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
            InvalidateDependentProperties();
            Dispatcher.BeginInvoke((Action)RecheckPath);
        }

        private void RecheckPath()
        {
            if (pathsDirty)
            {
                pathsDirty = false;
                checker.Check(CompletePath);
            }
        }

        private void Write()
        {
            string args = "";
            if (SystemPathChanged) args += "/system " + PathAsCommandLineArgument(SystemPath);
            if (UserPathChanged) args += " /user " + PathAsCommandLineArgument(UserPath);

            if (!UAC.Relaunch(args, NeedsElevation))
                MessageBox.Show("The changes were NOT saved!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                Read();
        }

        /// <summary>
        /// The complete path as it would be searched by Windows
        /// </summary>
        /// <remarks>
        /// First the SYSTEM entries are searched, then the USER entries.
        /// </remarks>
        private IEnumerable<AnnotatedPathEntry> CompletePath
        {
            get { return SystemPath.Concat(UserPath); }
        }

        #region Dependency Properties
        public ObservableCollectionEx<AnnotatedPathEntry> SystemPath
        {
            get { return (ObservableCollectionEx<AnnotatedPathEntry>)GetValue(SystemPathProperty); }
            set { SetValue(SystemPathProperty, value); }
        }

        public static readonly DependencyProperty SystemPathProperty =
            DependencyProperty.Register("SystemPath", typeof(ObservableCollectionEx<AnnotatedPathEntry>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollectionEx<AnnotatedPathEntry>(),
                (obj, e) => { ((MainWindow)obj).InvalidateDependentProperties(); }));

        public ObservableCollectionEx<AnnotatedPathEntry> UserPath
        {
            get { return (ObservableCollectionEx<AnnotatedPathEntry>)GetValue(UserPathProperty); }
            set { SetValue(UserPathProperty, value); }
        }

        public static readonly DependencyProperty UserPathProperty =
            DependencyProperty.Register("UserPath", typeof(ObservableCollectionEx<AnnotatedPathEntry>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollectionEx<AnnotatedPathEntry>(),
                (obj, e) => { ((MainWindow)obj).InvalidateDependentProperties(); }));

        public BitmapSource ShieldIcon
        {
            get { return (BitmapSource)GetValue(ShieldIconProperty); }
            set { SetValue(ShieldIconProperty, value); }
        }

        public static readonly DependencyProperty ShieldIconProperty =
            DependencyProperty.Register("ShieldIcon", typeof(BitmapSource), typeof(MainWindow), new UIPropertyMetadata(null));

        public bool NeedsElevation
        {
            get { return !reg.IsSystemPathWritable && SystemPathChanged; }
        }

        #endregion

        /// <summary>
        /// Called when the user has changed the path lists, to force WPF to reevaluate properties that depend on the lists
        /// </summary>
        private void InvalidateDependentProperties()
        {
            var changed = PropertyChanged;
            if (changed == null) return;

            changed(this, new PropertyChangedEventArgs("SystemPathChanged"));
            changed(this, new PropertyChangedEventArgs("UserPathChanged"));
            changed(this, new PropertyChangedEventArgs("NeedsElevation"));
        }

        private bool SystemPathChanged
        {
            get { return !PathListEqual(reg.SystemPath, SystemPath); }
        }

        private bool UserPathChanged
        {
            get { return !PathListEqual(reg.UserPath, UserPath); }
        }

        /// <summary>
        /// Compare two path lists
        /// </summary>
        private bool PathListEqual(IEnumerable<PathEntry> original,ObservableCollectionEx<AnnotatedPathEntry> edited)
        {
            return original.Count() == edited.Count() && original.Zip(edited, (a, b) => a.SymbolicPath == b.SymbolicPath).All(_ => _);
        }

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
                var u = up.Where((p, index) => p.Exists && !cp.Take(sp.Count() + index).Contains(p));

                SetPaths(s, u);
            }
        }

        public Func <IDataObject, object> FileDropConverter
        {
            get
            {
                return data => {
                    string path = "...";
                    try
                    {
                        var d = data as System.Windows.DataObject;
                        if (d == null || !d.ContainsFileDropList() || d.GetFileDropList().Count == 0) return null;
    
                        path = d.GetFileDropList()[0];
                        if (File.Exists(path)) path = System.IO.Path.GetDirectoryName(path);
    
                        return new AnnotatedPathEntry(PathEntry.FromFilePath(path));
                    }
                    catch (Exception ex)
                    {
                        return new AnnotatedPathEntry(new PathEntry(string.Format("error dragging in {0}: {1}", path, ex.Message)));
                    }
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
            e.CanExecute = false;
            try
            {
                e.CanExecute = GetSelectedEntry(e) != null && Directory.Exists(GetSelectedEntry(e).Path.ActualPath);
            }
            catch (Exception ex)
            {
                Debug.Print("Exception in CanExplore: {0}", ex);
            }
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

        private string PathAsCommandLineArgument(IEnumerable<AnnotatedPathEntry> path)
        {
            string arg = string.Join(";", path);
            if (arg.Contains("\""))
                throw new InvalidDataException("Error saving: path string contains \" character");
            return "\"" + arg + "\"";
        }

        private IEnumerable<PathEntry> ParseCommandLinePath(string argument)
        {
            return argument.Split(';').Select(path => new PathEntry(path));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DoSave(object sender, ExecutedRoutedEventArgs e)
        {
            Write();
        }

        private void CanSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = UserPathChanged || SystemPathChanged;
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            var currentPaths = CompletePath.Select(_ => _.Path);

            var window = new ScanningWindow();
            var search = new SearchOperation("C:\\", 4, window);

            Task<IEnumerable<string>>.Factory.StartNew(search.Run);

            var result = window.ShowDialog();
            if (result.HasValue && result.Value)
            {
                UserPath.SupressNotification = true;
                search.Result
                    .Select(PathEntry.FromFilePath)
                    .Where(path => !currentPaths.Contains(path))
                    .Each(path => UserPath.Add(new AnnotatedPathEntry(path)));
                UserPath.SupressNotification = false;
            }
        }
        
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var entry = new AnnotatedPathEntry(PathEntry.FromFilePath(dialog.SelectedPath));
                    UserPath.Add(entry);
                }
            }
        }

        private void ShowIssues_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
