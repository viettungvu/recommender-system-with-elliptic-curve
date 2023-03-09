using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using RSECC;
using RSECC.Utils;
using RSES;
using RSModels;
using RSModels.Utils;
using RSService.models;
using RSUtils;

namespace RSService
{
    public class Elliptic : BaseRSService
    {

        private static string _elliptic_folder = Path.Combine(_data_folder, "elliptic");
        private static int _curve_property = (int)ThuocTinhHeThong.CURVE_TYPE_SEC160K1;
        private static CurveFp _curve = Curves.getCurveByType(CurveType.sec160k1);
        public static event EventHandler<LogEventArgs> UpdateState;

        private static readonly string _file_private_key = "PrivateKey.txt";
        private static readonly string _file_public_key = "PublicKey.txt";
        private static readonly string _file_share_key = "SharedKey.txt";
        private static readonly string _file_c_to_server = "CipherTextSendToServer.txt";
        private static readonly string _file_c_to_client = "CipherTextSendToClient.txt";

        private static bool _run_phase_1 = false;
        private static bool _run_phase_2 = false;
        private static bool _run_phase_3 = true;
        private static bool _run_phase_4 = true;
        public static void Run()
        {
            try
            {
                bool success = false;
                UpdateState?.Invoke(null, new LogEventArgs { message = "Service starting..." });
                int so_user = 943;
                int so_phim = 200;
                Stopwatch sw = Stopwatch.StartNew();

                int ns = so_phim * (so_phim + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[so_user, so_phim * so_user];
                for (int i = 0; i < so_user; i++)
                {
                    for (int j = 0; j < so_phim; j++)
                    {
                        Ri[i, j] = 0;
                    }
                }

                string[] data = ReadFileAsLine("Data2.200.txt");

                Parallel.ForEach(data, line =>
                {
                    string[] values = line.Split(',');
                    Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
                });
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

                ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
                ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

                BigInteger[,] ksuij = new BigInteger[so_user, nk];
                Point[,] KPUij = new Point[so_user, nk];
                #region Pha 1
                if (_run_phase_1)
                {
                    UpdateState?.Invoke(null, new LogEventArgs { message = "Phase 1 started at " + DateTime.Now.ToLongTimeString() });

                    sw.Start();
                    Parallel.For(0, so_user, i =>
                    {
                        Parallel.For(0, nk, j =>
                        {
                            BigInteger secret = RSECC.Utils.Integer.randomBetween(1, _curve.order - 1);
                            Point pub = EcdsaMath.Multiply(_curve.G, secret, _curve.order, _curve.A, _curve.P);
                            concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                            concurrent_2.Add(string.Format("{0},{1},{2},{3}", i, j, pub.x, pub.y));
                        });
                    });
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Phase 1 completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
                    LogInfo log_pharse_1 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_1,
                        so_phim = so_phim,
                        so_user = so_user,
                        type = TypeSolution.ELLIPTIC,
                        thuoc_tinh = new List<int> { _curve_property },
                    };
                    log_pharse_1.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_1);


                    WriteFile(_file_private_key, string.Join(Environment.NewLine, concurrent_1), false);
                    WriteFile(_file_public_key, string.Join(Environment.NewLine, concurrent_2), false);
                    GenericMethod.Clear(concurrent_1);
                    GenericMethod.Clear(concurrent_2);
                }
                #endregion

                #region Pha 2 Máy chủ thực hiện
                Point[] KPj = new Point[nk];
                if (_run_phase_2)
                {
                    UpdateState?.Invoke(null, new LogEventArgs { message = "Phase 2 started at " + DateTime.Now.ToLongTimeString() });

                    string[] PubK = ReadFileAsLine(_file_public_key);
                    Parallel.ForEach(PubK, line =>
                    {
                        string[] values = line.Split(',');
                        KPUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                    });


                    sw.Reset();
                    sw.Start();
                    Parallel.For(0, nk, j =>
                    {
                        KPj[j] = new Point(0, 0);
                        ParallelLoopResult res = Parallel.For(0, so_user, i =>
                        {
                            Point temp = KPj[j];
                            KPj[j] = EcdsaMath.Add(temp, KPUij[i, j], _curve.A, _curve.P);
                        });
                        if (res.IsCompleted)
                        {
                            concurrent_1.Add(string.Format("{0},{1},{2}", j, KPj[j].x, KPj[j].y));
                        }
                    });
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Phase 2 completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
                    LogInfo log_pharse_2 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_2,
                        so_phim = so_phim,
                        so_user = so_user,
                        type = TypeSolution.ELLIPTIC,
                        thuoc_tinh = new List<int> { _curve_property },
                    };
                    log_pharse_2.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_2);

                    WriteFile(_file_share_key, string.Join(Environment.NewLine, concurrent_1), false);
                    GenericMethod.Clear(concurrent_1);
                }


                #endregion

                #region Pha 3 Những người dùng Ui thực hiện
                Point[,] AUij = new Point[so_user, ns];
                if (_run_phase_3)
                {
                    //ns = 10;
                    UpdateState?.Invoke(null, new LogEventArgs { message = "Phase 3 started at " + DateTime.Now.ToLongTimeString() });
                    string[] shared_key = ReadFileAsLine(_file_share_key);

                    Parallel.ForEach(shared_key, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            KPj[int.Parse(values[0])] = new Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                        }
                    });

                    string[] data_pha1 = ReadFileAsLine(_file_private_key);
                    Parallel.ForEach(data_pha1, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            ksuij[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
                        }
                    });

                    sw.Reset();
                    sw.Start();
                    Parallel.For(0, so_user, i =>
                    {
                        int j = 0;
                        for (int t = 0; t < nk; t++)
                        {
                            for (int k = t + 1; k < nk; k++)
                            {
                                Point p1 = EcdsaMath.Multiply(_curve.G, Rns[i, j], _curve.order, _curve.A, _curve.P);
                                Point p2 = EcdsaMath.Multiply(KPj[t], ksuij[i, k], _curve.order, _curve.A, _curve.P);
                                Point p3 = EcdsaMath.Multiply(KPj[k], ksuij[i, t], _curve.order, _curve.A, _curve.P);
                                BigInteger tmp = BigInteger.Remainder(-p3.y, _curve.P);
                                if (tmp < 0)
                                {
                                    tmp += _curve.P;
                                }
                                Point invp3 = new Point(p3.x, tmp);
                                AUij[i, j] = EcdsaMath.Add(EcdsaMath.Add(p1, p2, _curve.A, _curve.P), invp3, _curve.A, _curve.P);
                                concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, AUij[i, j].x, AUij[i, j].y));
                                if (j == ns - 1) break;
                                else j++;
                            }
                            if (j == ns - 1) break;
                        }
                    });
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Phase 3 completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
                    LogInfo log_pharse_3 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_3,
                        so_phim = so_phim,
                        so_user = so_user,
                        type = TypeSolution.ELLIPTIC,
                        thuoc_tinh = new List<int> { _curve_property },
                    };
                    log_pharse_3.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_3);
                    WriteFile(_file_c_to_server, string.Join(Environment.NewLine, concurrent_1), false);
                    GenericMethod.Clear(concurrent_1);
                }


                #endregion

                #region Pha 4 Máy chủ thực hiện
                if (_run_phase_4)
                {
                    sw.Reset();
                    sw.Start();
                    Point[] Aj = new Point[so_user * ns];
                    string[] data_phase3 = ReadFileAsLine(_file_c_to_server);

                    Parallel.ForEach(data_phase3, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            AUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                        }
                    });

                    Parallel.For(0, ns, (j, _) =>
                    {
                        Aj[j] = new Point(0, 0);
                        ParallelLoopResult res = Parallel.For(0, so_user, (i) =>
                        {
                            Aj[j] = EcdsaMath.Add(Aj[j], AUij[i, j], _curve.A, _curve.P);
                        });
                        if (res.IsCompleted)
                        {
                            concurrent_1.Add(string.Format("{0},{1},{2}",j, Aj[j].x, Aj[j].y));
                        }
                    });
                    sw.Stop();
                    LogInfo log_pharse_4 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_4,
                        so_phim = so_phim,
                        so_user = so_user
                    };
                    log_pharse_4.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_4);
                    WriteFile(_file_c_to_client, string.Join(Environment.NewLine, concurrent_1), false);
                    GenericMethod.Clear(concurrent_1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }

            #endregion
        }


        public static int[] BRF(BigInteger[] xSj, BigInteger g, BigInteger p, int ns, int max)
        {
            int[] result = new int[ns];
            BigInteger tg = 1;
            int j = 0, i = 0, k;
            for (i = 0; i < max; i++)
            {
                for (k = 0; k < ns; k++)
                {
                    if (tg == xSj[k])
                    {
                        result[k] = i;
                        j++;
                        if (j == ns) break;
                    }
                }
                tg = tg * g % p;
            }
            return result;
        }//Dung
    }


}