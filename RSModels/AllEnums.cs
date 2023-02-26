using System;
using System.Collections.Generic;
using System.Text;

namespace RSModels
{
    public enum Rate
    {

    }
    public enum Phase
    {
        ALL = 0,
        PHASE_1 = 1,
        PHASE_2 = 2,
        PHASE_3 = 3,
        PHASE_4 = 4,
    }

    public enum TypeSolution
    {
        ALL=0,
        ELLIPTIC=1,
    }

    public enum ThuocTinhHeThong
    {
        CURVE_TYPE_SEC128R1 = 101,
        CURVE_TYPE_SEC160K1 = 102,
        CURVE_TYPE_SEC160R1 = 103,
        CURVE_TYPE_SEC192K1 = 104,
        CURVE_TYPE_SEC192R1 = 105,
        CURVE_TYPE_SEC224R1 = 106,
        CURVE_TYPE_SEC256R1 = 107,
        CURVE_TYPE_SEC256K1 = 108,
    }
}
