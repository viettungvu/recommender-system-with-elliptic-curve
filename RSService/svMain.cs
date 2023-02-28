using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace RSService
{
    public partial class svMain : ServiceBase
    {
        const long interval = 10000000;
        private static Timer _timer = new Timer();
        private static readonly ILogger _log = LoggerFactory.Create(
            c =>
        c.SetMinimumLevel(LogLevel.Debug)).CreateLogger(typeof(svMain));
        public svMain()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Interval = interval;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Elliptic.Run();
            }
            catch (Exception)
            {

                throw;
            }
            throw new NotImplementedException();
        }

        protected override void OnStop()
        {
        }
    }
}
