using System.Numerics;


namespace RSECC
{

    public static class EcdsaMath
    {

        #region Old
        public static Point Multiply(Point p, BigInteger n, BigInteger N, BigInteger A, BigInteger P)
        {
            //Fast way to multily point and scalar in elliptic curves

            //:param p: First Point to mutiply
            //:param n: Scalar to mutiply
            //: param order: Order of the elliptic curve
            // : param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
            //:param A: Coefficient of the first-order term of the equation Y ^ 2 = X ^ 3 + A * X + B(mod p)
            //:return: Point that represents the sum of First and Second Point

            return fromJacobian(jacobianMultiply(toJacobian(p), n, N, A, P), P);
        }

        public static Point Add(Point p, Point q, BigInteger A, BigInteger P)
        {
            //Fast way to add two points in elliptic curves

            //:param p: First Point you want to add
            //:param q: Second Point you want to add
            //:param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
            //:param A: Coefficient of the first-order term of the equation Y ^ 2 = X ^ 3 + A * X + B(mod p)
            //:return: Point that represents the sum of First and Second Point

            return fromJacobian(jacobianAdd(toJacobian(p), toJacobian(q), A, P), P);
        }

        public static BigInteger Inv(BigInteger x, BigInteger n)
        {
            //Extended Euclidean Algorithm.It's the 'division' in elliptic curves

            //:param x: Divisor
            //: param n: Mod for division
            //:return: Value representing the division

            if (x.IsZero)
            {
                return 0;
            }

            BigInteger lm = BigInteger.One;
            BigInteger hm = BigInteger.Zero;
            BigInteger low = Utils.Integer.modulo(x, n);
            BigInteger high = n;
            BigInteger r, nm, newLow;

            while (low > 1)
            {
                r = high / low;

                nm = hm - lm * r;
                newLow = high - low * r;

                high = low;
                hm = lm;
                low = newLow;
                lm = nm;
            }

            return Utils.Integer.modulo(lm, n);

        }

        private static Point toJacobian(Point p)
        {
            //Convert point to Jacobian coordinates

            //: param p: First Point you want to Add
            //:return: Point in Jacobian coordinates

            return new Point(p.x, p.y, 1);
        }

        private static Point fromJacobian(Point p, BigInteger P)
        {
            //Convert point back from Jacobian coordinates

            //:param p: First Point you want to Add
            //:param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
            //:return: Point in default coordinates

            BigInteger z = Inv(p.z, P);

            return new Point(
                Utils.Integer.modulo(p.x * BigInteger.Pow(z, 2), P),
                Utils.Integer.modulo(p.y * BigInteger.Pow(z, 3), P)
            );
        }

        private static Point jacobianDouble(Point p, BigInteger A, BigInteger P)
        {
            //Double a point in elliptic curves

            //:param p: Point you want to double
            //:param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
            //:param A: Coefficient of the first-order term of the equation Y ^ 2 = X ^ 3 + A * X + B(mod p)
            //:return: Point that represents the sum of First and Second Point

            if (p.y.IsZero)
            {
                return new Point(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero);
            }

            BigInteger ysq = Utils.Integer.modulo(BigInteger.Pow(p.y, 2), P);
            BigInteger S = Utils.Integer.modulo(4 * p.x * ysq, P);
            BigInteger M = Utils.Integer.modulo(3 * BigInteger.Pow(p.x, 2) + A * BigInteger.Pow(p.z, 4), P);

            BigInteger nx = Utils.Integer.modulo(BigInteger.Pow(M, 2) - 2 * S, P);
            BigInteger ny = Utils.Integer.modulo(M * (S - nx) - 8 * BigInteger.Pow(ysq, 2), P);
            BigInteger nz = Utils.Integer.modulo(2 * p.y * p.z, P);

            return new Point(nx, ny, nz);
        }

        private static Point jacobianAdd(Point p, Point q, BigInteger A, BigInteger P)
        {
            // Add two points in elliptic curves

            // :param p: First Point you want to Add
            // :param q: Second Point you want to Add
            // :param P: Prime number in the module of the equation Y^2 = X^3 + A*X + B (mod p)
            // :param A: Coefficient of the first-order term of the equation Y^2 = X^3 + A*X + B (mod p)
            // :return: Point that represents the sum of First and Second Point

            if (p.y.IsZero)
            {
                return q;
            }
            if (q.y.IsZero)
            {
                return p;
            }

            BigInteger U1 = Utils.Integer.modulo(p.x * BigInteger.Pow(q.z, 2), P);
            BigInteger U2 = Utils.Integer.modulo(q.x * BigInteger.Pow(p.z, 2), P);
            BigInteger S1 = Utils.Integer.modulo(p.y * BigInteger.Pow(q.z, 3), P);
            BigInteger S2 = Utils.Integer.modulo(q.y * BigInteger.Pow(p.z, 3), P);

            if (U1 == U2)
            {
                if (S1 != S2)
                {
                    return new Point(BigInteger.Zero, BigInteger.Zero, BigInteger.One);
                }
                return jacobianDouble(p, A, P);
            }

            BigInteger H = U2 - U1;
            BigInteger R = S2 - S1;
            BigInteger H2 = Utils.Integer.modulo(H * H, P);
            BigInteger H3 = Utils.Integer.modulo(H * H2, P);
            BigInteger U1H2 = Utils.Integer.modulo(U1 * H2, P);
            BigInteger nx = Utils.Integer.modulo(BigInteger.Pow(R, 2) - H3 - 2 * U1H2, P);
            BigInteger ny = Utils.Integer.modulo(R * (U1H2 - nx) - S1 * H3, P);
            BigInteger nz = Utils.Integer.modulo(H * p.z * q.z, P);

            return new Point(nx, ny, nz);
        }

        private static Point jacobianMultiply(Point p, BigInteger n, BigInteger N, BigInteger A, BigInteger P)
        {
            // Multily point and scalar in elliptic curves

            // :param p: First Point to mutiply
            // :param n: Scalar to mutiply
            // :param order: Order of the elliptic curve
            // :param P: Prime number in the module of the equation Y^2 = X^3 + A*X + B (mod p)
            // :param A: Coefficient of the first-order term of the equation Y^2 = X^3 + A*X + B (mod p)
            // :return: Point that represents the sum of First and Second Point

            if (p.y.IsZero | n.IsZero)
            {
                return new Point(BigInteger.Zero, BigInteger.Zero, BigInteger.One);
            }

            if (n.IsOne)
            {
                return p;
            }

            if (n < 0 | n >= N)
            {
                return jacobianMultiply(p, Utils.Integer.modulo(n, N), N, A, P);
            }

            if (Utils.Integer.modulo(n, 2).IsZero)
            {
                return jacobianDouble(jacobianMultiply(p, n / 2, N, A, P), A, P);
            }

            // (n % 2) == 1:
            return jacobianAdd(
                jacobianDouble(
                    jacobianMultiply(
                        p,
                        n / 2,
                        N,
                        A,
                        P
                    ),
                    A,
                    P
                ),
                p,
                A,
                P
            );

        }
        #endregion

        #region Speed up

        private static BigInteger pow(BigInteger value, int exp)
        {
            return BigInteger.Pow(value, exp);
        }
        ///Fast 2^2P
        public static Point Fast4P(Point P, BigInteger a, BigInteger p)
        {
            Point fourP = new Point(BigInteger.Zero, BigInteger.Zero);
            BigInteger num1 = 3 * pow(P.x, 2) + a;  //3x1^2+a
            BigInteger q = 2 * P.x * pow(2 * P.y, 2) * num1 - 2 * pow(num1, 3) - pow(2 * P.y, 4);
            BigInteger U = 2 * P.y * q;
            BigInteger W = 3 * pow(pow(num1, 2) - 2 * P.x * pow(2 * P.y, 2), 2) + a * pow(2 * P.y, 4);
            fourP.x = (pow(W, 2) - 2 * pow(q, 2) * pow(num1, 2) + 4 * P.x * pow(U, 2)) * Inv(pow(U, 2), p);
            return fourP;
        }
        #endregion
    }
}
