using System.CodeDom;
using System.Numerics;


namespace RSECC
{

    public class Point
    {

        public BigInteger x { get; set; }
        public BigInteger y { get; set; }
        public BigInteger z { get; set; }

        public Point(BigInteger x, BigInteger y, BigInteger? z = null)
        {
            BigInteger zeroZ = z ?? BigInteger.Zero;

            this.x = x;
            this.y = y;
            this.z = zeroZ;
        }

        public bool isAtInfinity()
        {
            return y == 0;
        }

        public override string ToString()
        {
            return string.Format("{0};{1}", x, y);
        }
    }
}
