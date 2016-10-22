using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using ExecuteCommand;
using FirstFloor.ModernUI.Windows.Controls;
using Runny.Model;

namespace Runny
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public DateTime CurrentTime { set; get; }

        public DateTime LastTime { get; set; }

        public List<Job> ExecutedJobsDaily { get; set; }

        public List<Job> ExecutedJobsMaster { get; set; }

        /// <summary>
        /// The function checks whether the current process is run as administrator.
        /// In other words, it dictates whether the primary access token of the 
        /// process belongs to user account that is a member of the local 
        /// Administrators group and it is elevated.
        /// </summary>
        /// <returns>
        /// Returns true if the primary access token of the process belongs to user 
        /// account that is a member of the local Administrators group and it is 
        /// elevated. Returns false if the token does not.
        /// </returns>
        internal bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }



        // 5/23/2013 6:55:24 PM

        /*
            Date	    {5/23/2013 12:00:00 AM}	    System.DateTime
            Day	        23	                        int
            DayOfWeek	Thursday	                System.DayOfWeek
            DayOfYear	143	                        int
            Hour	    18	                        int
            Kind	    Local	                    System.DateTimeKind
            Millisecond	553	                        int
            Minute	    55	                        int
            Month	    5	                        int
            Second	    24	                        int
            Ticks	    635049321245532856	        long
    +		TimeOfDay	{18:55:24.5532856}	        System.TimeSpan
                Days	            0	                int
                Hours	            18	                int
                Milliseconds	    553	                int
                Minutes	            55	                int
                Seconds	            24	                int
                Ticks	            681245532856	    long
                TotalDays	        0.7884786259907407	double
                TotalHours	        18.923487023777778	double
                TotalMilliseconds	68124553.2856	    double
                TotalMinutes	    1135.4092214266666	double
                TotalSeconds	    68124.553285599992	double
            Year	    2013	                    int
         */

        public MainWindow()
        {
            var isRunElevated = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("isRunElevated"));

            if (isRunElevated)
                ElevatePermissions();

            InitializeComponent();

            if (isRunElevated)
            {
                if (!IsRunAsAdmin())
                    Application.Current.Shutdown();
            }

            //init lists
            ExecutedJobsDaily = new List<Job>();
            ExecutedJobsMaster = new List<Job>();

            Globals.Current.JobsInProgress = new ObservableCollection<Job>();

            //endless loop
            int i = -1;
            while (i == -1)
            {
                // set current time per second once
                CurrentTime = DateTime.Now;

                //get jobs
                LoadXml();

                //reset executed command list per 24 hr period
                if (CurrentTime.ToLongTimeString() == DateTime.Parse("00:00:00").ToLongTimeString())
                    ExecutedJobsDaily = new List<Job>();
                
                //get commands to run and execute them
                foreach (var job in Globals.Current.Jobs)
                {
                    //get execution time
                    var jobTime = new DateTime();
                    try
                    {
                        jobTime = DateTime.Parse(job.Time);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(Properties.Resources.MainWindow_MainWindow_Job_time__0__is_in_improper_format, job.Time);
                    }

                    bool jobAlreadyExecuted = ExecutedJobsDaily.Any(theJob =>(theJob.Time == job.Time) && 
                        (theJob.Command == job.Command) && (theJob.CommandParams == job.CommandParams) && 
                        theJob.Comment == job.Comment);

                    bool jobAlreadyListedInMaster = ExecutedJobsMaster.Any(theJob => (theJob.Time == job.Time) && 
                        (theJob.Command == job.Command) && (theJob.CommandParams == job.CommandParams) && 
                        theJob.Comment == job.Comment);
                    
                    // check if command has already been executed within the current minute
                    if (jobAlreadyExecuted && (jobTime.ToShortTimeString() == CurrentTime.ToShortTimeString()))
                        continue;

                    // should we loop the job or not? - if loop is set to 0, then check to see if it's already listed in the master list
                    if (Convert.ToInt32(job.Loop) == 0 && jobAlreadyListedInMaster)
                        continue;

                    // try to execute
                    if (job.ExecuteOnTimer == 0 && CurrentTime.ToLongTimeString() == jobTime.ToLongTimeString())
                    {
                        ExecuteCmd.ExecuteCommandAsync(job.Command);

                        // add executed job to the list
                        if (!jobAlreadyExecuted)
                            ExecutedJobsDaily.Add(job);

                        //add job to a master list every day only
                        if (!jobAlreadyExecuted)
                            ExecutedJobsMaster.Add(job);
                    }
                    else if (job.ExecuteOnTimer == 1) // execute on the hour
                    {
                        //note: this logic can probably be optimized...

                        // job in progress
                        var job1 = Globals.Current.JobsInProgress.FirstOrDefault(
                                theJob => (theJob.Time == job.Time) &&
                                          (theJob.Command == job.Command) &&
                                          (theJob.CommandParams == job.CommandParams) &&
                                          theJob.Comment == job.Comment);

                        // job from regular job queue
                        var job2 = Globals.Current.Jobs.FirstOrDefault(theJob => (theJob.Time == job.Time) && 
                            (theJob.Command == job.Command) && (theJob.CommandParams == job.CommandParams) && 
                            theJob.Comment == job.Comment);

                        // favor the job in progress over job queue job
                        var jobInProgress = job1 ?? job2;

                        if (jobInProgress != null)
                        {
                            if (jobInProgress.NonTwentyFourHourExecuteOnSecondsTimer == null)
                                jobInProgress.NonTwentyFourHourExecuteOnSecondsTimer = CurrentTime.Minute*60 + CurrentTime.Second;

                            var currentJobTimer = jobTime.Minute*60 + jobTime.Second;

                            if (jobInProgress.NonTwentyFourHourExecuteOnSecondsTimer >= currentJobTimer)
                            {
                                ExecuteCmd.ExecuteCommandAsync(job.Command);
                                jobInProgress.NonTwentyFourHourExecuteOnSecondsTimer = 0;
                            }
                            else
                            {
                                jobInProgress.NonTwentyFourHourExecuteOnSecondsTimer++;
                            }

                            if (Globals.Current.JobsInProgress != null)
                            {
                                //update timer
                                var jobToUpdate =
                                    Globals.Current.JobsInProgress.FirstOrDefault(
                                        theJob => (theJob.Time == jobInProgress.Time) &&
                                                  (theJob.Command == jobInProgress.Command) &&
                                                  (theJob.CommandParams == jobInProgress.CommandParams) &&
                                                  theJob.Comment == jobInProgress.Comment);

                                if (jobToUpdate != null)
                                {
                                    //update timer
                                    jobToUpdate.NonTwentyFourHourExecuteOnSecondsTimer =
                                        jobInProgress.NonTwentyFourHourExecuteOnSecondsTimer;
                                }
                                else
                                {
                                    //1st run through
                                    Globals.Current.JobsInProgress.Add(jobInProgress);
                                }
                            }
                            else
                            {
                                //this probably never gets hit
                                Globals.Current.JobsInProgress.Add(jobInProgress);
                            }
                        }
                    }
                }

                // delay 1 second
                Thread.Sleep(1000);

                LastTime = CurrentTime;
            }
        }

        private void LoadXml()
        {
            Globals.Current.CurrentSchedule = new Scheduler();
            var serializer = new XmlSerializer(typeof(Scheduler));

            var fs = new FileStream(ConfigurationManager.AppSettings.Get("xmlFileName"), FileMode.Open);
            Globals.Current.CurrentSchedule = (Scheduler)serializer.Deserialize(fs);
            Globals.Current.Jobs = Globals.Current.CurrentSchedule.Jobs;
            fs.Dispose();
        }

        private void ElevatePermissions()
        {
            // Elevate the process if it is not run as administrator.
            if (!IsRunAsAdmin())
            {
                // Launch itself as administrator
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = System.Reflection.Assembly.GetEntryAssembly().Location;  // or System.Windows.Forms.Application.ExecutablePath ;
                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                }
                catch
                {
                    // The user refused to allow privileges elevation.;
                    // Do nothing and return directly ...
                    return;
                }

                Application.Current.Shutdown();  // Quit itself
            }
            else
            {
                Console.WriteLine("UAC: The process is running as administrator.");
            }
        }
    }
}
