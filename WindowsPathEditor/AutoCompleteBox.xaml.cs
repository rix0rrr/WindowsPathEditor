using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowsPathEditor
{
    /// <summary>
    /// Interaction logic for AutoCompleteBox.xaml
    /// </summary>
    public partial class AutoCompleteBox
    {
        private IDisposable subscription;

        public AutoCompleteBox()
        {
            InitializeComponent();
        }

        public void SetCompleteProvider(Func<string, IEnumerable<object>> provider)
        {
            if (subscription != null) subscription.Dispose();

            var changes = Observable.FromEventPattern<TextChangedEventArgs>(textBox, "TextChanged")
                .Select(e => ((TextBox)e.Sender).Text)
                .Where(txt => txt != "");

            var search = Observable.ToAsync<string, IEnumerable<object>>(provider);

            var results = from s in changes
                          from r in search(s).TakeUntil(changes)
                          select r;

            subscription = results.ObserveOnDispatcher()
                .Subscribe(res =>
            {
                popup.IsOpen = true;
                if (res.Count() > 0)
                    suggestionList.ItemsSource = res;
                else
                    suggestionList.ItemsSource = new string[] { "(no matches)" };
            });
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { textBox.Text = ""; popup.IsOpen = false; }
            if (textBox.Text == "") popup.IsOpen = false;
        }
    }
}