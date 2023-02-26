using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSService.models
{
    public class LogEventArgs : EventArgs
    {
        //public long excute_time { get; set; }
        //public string event_name { get; set; }
        public string message { get; set; }

    }
}
