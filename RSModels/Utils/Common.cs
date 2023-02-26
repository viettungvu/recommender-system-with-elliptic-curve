using System;
using System.Collections.Generic;
using System.Text;

namespace RSModels.Utils
{
    public static class Common
    {
        public static void SetMetadata(this BaseModel model, bool is_update = false)
        {
            if (is_update)
            {
                model.ngay_sua = 0;
            }
            else
            {
                model.ngay_sua = 0;
                model.ngay_tao = 0;
            }
        }
    }
}
