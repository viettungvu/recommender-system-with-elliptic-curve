using System;
using System.Collections.Generic;
using System.Text;

namespace RSModels
{
    public class BaseModel
    {
        public string id { get; set; }
        public string nguoi_tao { get; set; } = "vietungtvhd";
        public string nguoi_sua { get; set; } = "viettungtvhd";
        public long ngay_sua { get; set; } = 0;
        public List<string> thuoc_tinh { get; set; }
    }
}
