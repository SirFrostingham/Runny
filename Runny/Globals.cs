using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Runny.Annotations;
using Runny.Model;

namespace Runny
{
    public class Globals : INotifyPropertyChanged
    {
        private static Globals _globals;

        public static Globals Current
        {
            get
            {
                if (_globals == null)
                    _globals = new Globals();

                return _globals;
            }
        }

        private Scheduler _currentSchedule;
        public Scheduler CurrentSchedule
        {
            get { return _currentSchedule; }
            set
            {
                _currentSchedule = value;
                OnPropertyChanged("CurrentSchedule");
                OnPropertyChanged("Jobs");
                OnPropertyChanged("JobsInProgress");
            }
        }

        private List<Job> _jobs;

        public List<Job> Jobs
        {
            get { return _jobs; }
            set
            {
                _jobs = value;
                OnPropertyChanged("CurrentSchedule");
                OnPropertyChanged("Jobs");
                OnPropertyChanged("JobsInProgress");
            }
        }

        private ObservableCollection<Job> _jobsInProgress;

        public ObservableCollection<Job> JobsInProgress
        {
            get { return _jobsInProgress; }
            set
            {
                _jobsInProgress = value;
                OnPropertyChanged("CurrentSchedule");
                OnPropertyChanged("Jobs");
                OnPropertyChanged("JobsInProgress");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
