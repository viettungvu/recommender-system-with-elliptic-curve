using System;
using System.Collections.Generic;
using System.Text;

namespace RSModels
{
    public class Rating : BaseModel
    {
        public int nguoi_dung { get; set; }
        public int muc_tin { get; set; }
        public int xep_hang { get; set; }
    }
}
