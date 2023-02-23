using System;
using System.Collections.Generic;
using System.Text;

namespace RSModels
{
    public class TaiKhoan : BaseModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string fullname { get; set; }
        public string avatar { get; set; }
    }
}
