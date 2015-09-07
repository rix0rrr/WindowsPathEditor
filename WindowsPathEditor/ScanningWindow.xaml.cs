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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for ScanningWindow.xaml
    /// </summary>
    public partial class ScanningWindow : Window, IReportProgress, INotifyPropertyChanged
    {
        private bool searching = true;

        public ScanningWindow()
        {
            InitializeComponent();
            Paths.Clear();
        }

        public void ReportProgress(string progress)
        {
            Dispatcher.BeginInvoke((Action)(() => { currentDirectory.Content = progress; }));
        }

        public void Done()
        {
            Dispatcher.BeginInvoke((Action)(() => {
                whatLabel.Content = "Done. Select paths to add.";
                SearchDone();
            }));
        }

        public void Begin()
        {
        }

        public bool Cancelled { get; private set; }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!searching) 
            {
                DialogResult = false;
                Close();
                return;
            }

            Cancelled = true;
            Dispatcher.BeginInvoke((Action)(() => {
                whatLabel.Content = "Done. Select paths to add.";
                SearchDone();
            }));
        }

        private void SearchDone()
        {
            cancelButton.Content = "Close";
            searching = false;
            progressBar1.IsIndeterminate = false;
            progressBar1.Maximum = 1;
            progressBar1.Value = 1;
            currentDirectory.Content = "";
        }

        public void FoundCandidate(string path)
        {
            Dispatcher.BeginInvoke((Action)(() => { Paths.Add(new SelectablePath(path, true)); }));
        }

        private void searchBox_Loaded(object sender, RoutedEventArgs e)
        {

        }

        #region Dependency Properties

        public ObservableCollection<SelectablePath> Paths
        {
            get { return (ObservableCollection<SelectablePath>)GetValue(PathsProperty); }
            set { SetValue(PathsProperty, value); }
        }

        public static readonly DependencyProperty PathsProperty =
            DependencyProperty.Register("Paths",
                typeof(ObservableCollection<SelectablePath>),
                typeof(ScanningWindow),
                new UIPropertyMetadata(new ObservableCollection<SelectablePath>(), (obj, e) => { ((ScanningWindow)obj).InvalidateDependentProperties(); }));

        #endregion

        private void InvalidateDependentProperties()
        {
            var changed = PropertyChanged;
            if (changed == null) return;

            changed(this, new PropertyChangedEventArgs("Paths"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
