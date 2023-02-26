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
namespace RSService
{
    public partial class Form1 : Form
    {
        private static System.Timers.Timer _timer = null;
        private static double _time_overlapse = 10;
        private static bool can_run = true;
        public Form1()
        {
            InitializeComponent();
            _init();
        }


        private void _init()
        {
            _timer = new System.Timers.Timer();
            double.TryParse(System.Configuration.ConfigurationManager.AppSettings["TimeElapseService"], out _time_overlapse);
            _timer.Interval = (int)_time_overlapse * 3600;
            _timer.AutoReset = true;
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (can_run == true)
            {
                LogUtil.Run();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _timer.Start();
        }
    }
}
