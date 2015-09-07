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

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for DiffWindow.xaml
    /// </summary>
    public partial class DiffWindow : Window
    {
        public DiffWindow()
        {
            InitializeComponent();

            Changes = new ObservableCollection<DiffPath>();
        }

        #region Dependency Properties

        public ObservableCollection<DiffPath> Changes
        {
            get { return (ObservableCollection<DiffPath>)GetValue(ChangesProperty); }
            set { SetValue(ChangesProperty, value); }
        }

        public static readonly DependencyProperty ChangesProperty =
            DependencyProperty.Register("Changes",
                typeof(ObservableCollection<DiffPath>),
                typeof(DiffWindow));

        #endregion

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
