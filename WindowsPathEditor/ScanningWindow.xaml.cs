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
