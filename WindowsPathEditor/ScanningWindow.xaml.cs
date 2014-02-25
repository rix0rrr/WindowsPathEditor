using System;
using System.Windows;

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for ScanningWindow.xaml
    /// </summary>
    public partial class ScanningWindow : Window, IReportProgress
    {
        public ScanningWindow()
        {
            InitializeComponent();
        }

        public void ReportProgress(string progress)
        {
            Dispatcher.BeginInvoke((Action)(() => { currentDirectory.Content = progress; }));
        }

        public void Done()
        {
            Dispatcher.BeginInvoke((Action)Close);
        }

        public void Begin()
        {
            Dispatcher.BeginInvoke((Action)(() => { ShowDialog(); }));
        }

        public bool Cancelled { get; private set; }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancelled = true;
        }
    }
}