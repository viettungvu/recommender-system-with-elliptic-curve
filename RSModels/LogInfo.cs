using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RSModels
{
    public class LogInfo : BaseModel
    {
        public int so_user { get; set; }
        public int so_phim { get; set; }
        public long thoi_gian { get; set; }
        public ECCPhase pharse { get; set; }
        //public long thoi_gian_chuyen_doi_data { get; set; }
        public TypeSolution type { get; set; }
    }

    public class FileData
    {
        public int i { get; set; }
        public int j { get; set; }

        public BigInteger x { get; set; }

        public BigInteger y { get; set; }
    }
}
