using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using Nest;
using RSService.models;

namespace RSService
{
    public partial class Form1 : Form
    {
        private static System.Timers.Timer _timer = null;
        private static double _time_overlapse = 10;
        private static bool can_run = true;
        private static BackgroundWorker _worker = null;
        private static int _test_number = 100;
        public Form1()
        {
            InitializeComponent();
            _init();
            setting();
        }

        private void setting()
        {
            tbxReport.Font = new Font("SegoeUI", 10, FontStyle.Regular);
        }

        private void _init()
        {
            initWorker();
            _timer = new System.Timers.Timer();
            double.TryParse(System.Configuration.ConfigurationManager.AppSettings["TimeElapseService"], out _time_overlapse);
            _timer.Interval = (int)_time_overlapse * 3600;
            _timer.AutoReset = true;
            _timer.Elapsed += _timer_Elapsed;
        }

        private void initWorker()
        {
            _worker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _worker.DoWork += _worker_DoWork;
            _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
            _worker.ProgressChanged += _worker_ProgressChanged;
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            setLabel(tbxReport, e.UserState.ToString());
        }

        private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.Dispose();
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker.CancellationPending != true)
            {
                worker.ReportProgress(0, "Bắt đầu xử lí");
                Elliptic.UpdateState += UpdateStatus;
                //Elliptic.Run();
                Elliptic.RunWithoutStopWatch();
                worker.ReportProgress(0, "Xử lí xong");
            }
        }

        private void UpdateStatus(object sender, LogEventArgs e)
        {
            _worker.ReportProgress(0, e.message);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (can_run == true)
            {
                Elliptic.Run();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            initWorker();
            _worker.RunWorkerAsync();
            //_timer.Start();
        }



        private void setLabel(Control ctl, string text, bool is_append = true)
        {
            if (is_append)
            {
                ctl.Text += text + Environment.NewLine;
            }
            else
            {
                ctl.Text = text;
            }
        }
    }
}
