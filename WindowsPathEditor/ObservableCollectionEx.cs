using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WindowsPathEditor
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        private bool _notificationSupressed = false;
        private bool _supressNotification = false;

        public ObservableCollectionEx() 
        {
        }

        public ObservableCollectionEx(IEnumerable<T> xs) : base(xs)
        {
        }

        public bool SupressNotification
        {
            get
            {
                return _supressNotification;
            }
            set
            {
                _supressNotification = value;
                if (_supressNotification == false && _notificationSupressed)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    _notificationSupressed = false;
                }
            }
        }
    
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SupressNotification)
            {
                _notificationSupressed = true;
                return;
            }
            base.OnCollectionChanged(e);
        }
    }
}
