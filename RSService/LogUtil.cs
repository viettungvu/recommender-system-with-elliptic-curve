using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RSECC;
using RSES;
using RSModels;
using RSModels.Utils;

namespace RSService
{
    public static class LogUtil
    {
        private static Random _rd = new Random();
        private static int _curve_property = (int)ThuocTinhHeThong.CURVE_TYPE_SEC160K1;
        private static CurveFp _curve = Curves.getCurveByType(CurveType.sec160k1);
        public static void Run()
        {
            int so_user = 943;
            int so_phim = 200;
            int max = 5;
            Stopwatch sw = Stopwatch.StartNew();

            int ns = so_phim * (so_phim + 5) / 2;
            int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
            int[,] Ri = new int[so_user, 200];
            for (int i = 0; i < so_user; i++)
            {
                for (int j = 0; j < so_phim; j++)
                {
                    Ri[i, j] = 0;
                }
            }

            //string data_file = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "data", "Data2.200.txt");
            //string[] raw_data = File.ReadAllLines(data_file);
            //List<RawData> data = new List<RawData>();
            //foreach (string line in raw_data)
            //{
            //    string[] values = line.Split(',');
            //    //Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
            //    data.Add(new RawData
            //    {
            //        muc_tin_thu_j = int.Parse(values[1]) - 1,
            //        nguoi_dung_thu_i = int.Parse(values[0]) - 1,
            //        xep_hang= int.Parse(values[2]),
            //    });
            //}

            //bool success=RatingRepository.Instance.IndexMany(data);
            var dsach_raw_data = RatingRepository.Instance.GetAll();
            //return;
            int[,] Rns = new int[so_user, ns];
            for (int i = 0; i < so_user; i++)
            {
                for (int j = 0; j < so_phim; j++)
                {
                    Rns[i, j] = Ri[i, j];
                }
                for (int j = so_phim; j < 2 * so_phim; j++)
                {
                    if (Ri[i, j - so_phim] == 0) Rns[i, j] = 0;
                    else Rns[i, j] = 1;
                }
                for (int j = 2 * so_phim; j < 3 * so_phim; j++)
                {
                    Rns[i, j] = Ri[i, j - 2 * so_phim] * Ri[i, j - 2 * so_phim];
                }

                int t = 3 * so_phim;
                for (int t2 = 0; t2 < so_phim - 1; t2++)
                {
                    for (int t22 = t2 + 1; t22 < so_phim; t22++)
                    {
                        Rns[i, t] = Ri[i, t2] * Ri[i, t22];
                        t++;
                    }
                }
            }

            #region Pha 1
            BigInteger[,] ksuij = new BigInteger[so_user, nk];
            sw.Start();
            Point[,] KPUij = new Point[so_user, nk];
            Parallel.For(0, so_user, i =>
            {
                Parallel.For(0, nk, j =>
                {
                    PrivateKey private_key = new PrivateKey(CurveType.sec160k1);
                    ksuij[i, j] = private_key.secret;
                    PublicKey public_key = private_key.publicKey();
                    KPUij[i, j] = public_key.point;
                });
            });
            sw.Stop();
            LogInfo log_pharse_1 = new LogInfo()
            {
                thoi_gian = sw.ElapsedMilliseconds,
                pharse = Phase.PHASE_1,
                so_phim = so_phim,
                so_user = so_user,
                type = TypeSolution.ELLIPTIC,
                thuoc_tinh = new List<int> { _curve_property },
            };
            log_pharse_1.SetMetadata();
            bool success = LoggerRepository.Instance.Index(log_pharse_1);
            #endregion
            
            #region Pha 2 Máy chủ thực hiện
            sw.Start();
            Point[] KPj = new Point[nk];
            Parallel.For(0, nk, j =>
            {
                KPj[j] = new Point(0, 0);
                Parallel.For(0, so_user, i =>
                {
                    KPj[j] = EcdsaMath.add(KPj[j], KPUij[i, j], _curve.A, _curve.P);
                });
            });
            sw.Stop();
            LogInfo log_pharse_2 = new LogInfo()
            {
                thoi_gian = sw.ElapsedMilliseconds,
                pharse = Phase.PHASE_2,
                so_phim = so_phim,
                so_user = so_user,
                type = TypeSolution.ELLIPTIC,
                thuoc_tinh = new List<int> { _curve_property },
            };
            log_pharse_2.SetMetadata();
            success = LoggerRepository.Instance.Index(log_pharse_2);
            #endregion

            #region Pha 3
            Point[,] AUij = new Point[so_user, ns];
            for (int i = 0; i < so_user; i++)
            {
                int j = 0;
                for (int t = 0; t < nk - 1; t++)
                {
                    for (int k = t + 1; k < nk; k++)
                    {
                        Point p1 = EcdsaMath.multiply(_curve.G, 2, _curve.order, _curve.A, _curve.P);
                        Point p2 = EcdsaMath.multiply(KPj[t], ksuij[i, k], _curve.order, _curve.A, _curve.P);
                        Point p3 = EcdsaMath.multiply(KPj[k], ksuij[i, t], _curve.order, _curve.A, _curve.P);
                        BigInteger tmp = BigInteger.Remainder(-p3.y, _curve.P);
                        if (tmp < 0)
                        {
                            tmp += _curve.P;
                        }
                        Point invp3 = new Point(p3.x, tmp);
                        AUij[i, j] = EcdsaMath.add(EcdsaMath.add(p1, p2, _curve.A, _curve.P), invp3, _curve.A, _curve.P);
                        if (j == ns - 1) break;
                        else j++;
                    }
                }
            }
            LogInfo log_pharse_3 = new LogInfo()
            {
                thoi_gian = sw.ElapsedMilliseconds,
                pharse = Phase.PHASE_3,
                so_phim = so_phim,
                so_user = so_user,
                type = TypeSolution.ELLIPTIC,
                thuoc_tinh = new List<int> { _curve_property },
            };
            success = LoggerRepository.Instance.Index(log_pharse_3);
            return;
            #endregion


            #region Pha 4
            //BigInteger[] A = new BigInteger[683];//[ns];
            //int tg, tg1;
            //for (int j = 1316; j < ns; j++)
            //{
            //    i = 1316;
            //    m = 683;
            //    j = 0;
            //    BigInteger[,] AU = new BigInteger[n, m];
            //    reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7. OurSolution - RS\0. Phase 3.txt");
            //    while (!reader1.EndOfStream)
            //    {
            //        var line = reader1.ReadLine();
            //        var values = line.Split(',');
            //        tg = int.Parse(values[1]);
            //        tg1 = int.Parse(values[0]);
            //        if ((tg >= i) && (tg1 <= 49) && (tg <= 1999))
            //        {
            //            AU[tg1, j] = BigInteger.Parse(values[2]);
            //            j++;
            //            if (j == m)
            //            {
            //                j = 0;
            //                if (tg1 == 49)
            //                    break;
            //            }
            //        }
            //    }
            //    reader1.Close();
            //}
            //LogInfo log_pharse_4 = new LogInfo()
            //{
            //    thoi_gian = sw.ElapsedMilliseconds,
            //    pharse = Phase.PHARSE_4,
            //    so_phim = so_phim,
            //    so_user = so_user
            //};
            //LoggerRepository.Instance.Index(log_pharse_4);
            #endregion
        }

    }
}