using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
using static System.Windows.Forms.AxHost;

namespace RSService
{
    public class Elliptic : BaseRSService
    {
        private static string _elliptic_folder = Path.Combine(_data_folder, "elliptic");
        private static int _curve_property = (int)ThuocTinhHeThong.CURVE_TYPE_SEC160K1;
        private static CurveFp _curve = Curves.getCurveByType(CurveType.sec160k1);
        public static event EventHandler<LogEventArgs> UpdateState;

        private static readonly string _key_user_prv = "0.1.KeyUserPrv.txt";
        private static readonly string _key_user_pub = "0.2.KeyUserPub.txt";
        private static readonly string _key_common = "0.3.KeyCommon.txt";
        private static readonly string _encrypt = "0.4.Encrypt.txt";
        private static readonly string _sum_encrypt = "0.5.SumEncrypt.txt";
        private static readonly string _get_sum_encrypt = "0.6.Sum.txt";

        private static bool _run_phase_1 = false;
        private static bool _run_phase_2 = false;
        private static bool _run_phase_3 = false;
        private static bool _run_phase_4 = true;
        private static bool _run_export_sum = true;

        private static int max = 5;
        public static void Run()
        {
            try
            {
                bool success = false;
                UpdateState?.Invoke(null, new LogEventArgs { message = "Service starting..." });
                int n = 943;
                int m = 200;
                Stopwatch sw = Stopwatch.StartNew();

                int ns = n * (n + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[n, m];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < m; j++)
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
                int[,] Rns = new int[n, ns];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < m; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                    }
                    for (int j = n; j < 2 * m; j++)
                    {
                        if (Ri[i, j - m] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                    }
                    for (int j = 2 * m; j < 3 * m; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * m] * Ri[i, j - 2 * m];
                    }

                    int t = 3 * m;
                    for (int t2 = 0; t2 < n - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < m; t22++)
                        {
                            Rns[i, t] = Ri[i, t2] * Ri[i, t22];
                            t++;
                        }
                    }
                }

                ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
                ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

                BigInteger[,] ksuij = new BigInteger[n, nk];
                Point[,] KPUij = new Point[n, nk];


                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                if (_run_phase_1)
                {
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đang chuẩn bị các khóa", DateTime.Now.ToLongTimeString()) });

                    sw.Start();
                    Parallel.For(0, n, i =>
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
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đã tạo xong các khóa - {1} giây", DateTime.Now.ToLongTimeString(), (double)sw.ElapsedMilliseconds / 1000) });
                    LogInfo log_pharse_1 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_1,
                        so_phim = n,
                        so_user = n,
                        type = TypeSolution.ELLIPTIC,
                        thuoc_tinh = new List<int> { _curve_property },
                    };
                    log_pharse_1.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_1);
                    WriteFile(_key_user_prv, string.Join(Environment.NewLine, concurrent_1), false);
                    WriteFile(_key_user_pub, string.Join(Environment.NewLine, concurrent_2), false);
                    GenericMethod.Clear(concurrent_1);
                    GenericMethod.Clear(concurrent_2);
                }
                #endregion

                #region Pha 2 Tính các khóa công khai dùng chung Máy chủ thực hiện
                Point[] KPj = new Point[nk];
                if (_run_phase_2)
                {
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đang tính khóa công khai dùng chung", DateTime.Now.ToLongTimeString()) });

                    string[] key_user_pub = ReadFileAsLine(_key_user_pub);
                    Parallel.ForEach(key_user_pub, line =>
                    {
                        string[] values = line.Split(',');
                        KPUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                    });

                    sw.Reset();
                    sw.Start();
                    Parallel.For(0, nk, j =>
                    {
                        KPj[j] = new Point(0, 0, 1);
                        for (int i = 0; i < n; i++)
                        {
                            Point temp = KPj[j];
                            KPj[j] = EcdsaMath.Add(temp, KPUij[i, j], _curve.A, _curve.P);
                        }
                        concurrent_1.Add(string.Format("{0},{1},{2}", j, KPj[j].x, KPj[j].y));
                    });
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đã tính các khóa công khai dùng chung - {0} giây", DateTime.Now.ToLongTimeString(), (double)sw.ElapsedMilliseconds / 1000) });
                    LogInfo log_pharse_2 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_2,
                        so_phim = n,
                        so_user = n,
                        type = TypeSolution.ELLIPTIC,
                        thuoc_tinh = new List<int> { _curve_property },
                    };
                    log_pharse_2.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_2);

                    WriteFile(_key_common, string.Join(Environment.NewLine, concurrent_1), false);
                    GenericMethod.Clear(concurrent_1);
                }


                #endregion

                #region Pha 3 Gửi dữ liệu Những người dùng Ui thực hiện
                n = 100;
                ns = n * (n + 5) / 2;
                nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                Point[,] AUij = new Point[n, ns];
                if (_run_phase_3)
                {
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đang gửi dữ liệu", DateTime.Now.ToLongTimeString()) });
                    string[] shared_key = ReadFileAsLine(_key_common);

                    Parallel.ForEach(shared_key, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            KPj[int.Parse(values[0])] = new Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                        }
                    });

                    string[] key_user_prv = ReadFileAsLine(_key_user_prv);
                    Parallel.ForEach(key_user_prv, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            ksuij[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
                        }
                    });

                    sw.Reset();
                    sw.Start();
                    Parallel.For(0, n, i =>
                    {
                        int j = 0;
                        for (int t = 0; t < nk - 1; t++)
                        {
                            for (int k = t + 1; k < nk; k++)
                            {
                                Point p1 = EcdsaMath.Multiply(_curve.G, Rns[i, j], _curve.order, _curve.A, _curve.P);
                                Point p2 = EcdsaMath.Multiply(KPj[t], ksuij[i, k], _curve.order, _curve.A, _curve.P);
                                Point p3 = EcdsaMath.Multiply(KPj[k], ksuij[i, t], _curve.order, _curve.A, _curve.P);
                                AUij[i, j] = EcdsaMath.Sub(EcdsaMath.Add(p1, p2, _curve.A, _curve.P), p3, _curve.A, _curve.P);
                                concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, AUij[i, j].x, AUij[i, j].y));
                                if (j == ns - 1) break;
                                else j++;
                            }
                            if (j == ns - 1) break;
                        }
                    });
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đã gửi dữ liệu - {1} giây", DateTime.Now.ToLongTimeString(), (double)sw.ElapsedMilliseconds / 1000) });
                    LogInfo log_pharse_3 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_3,
                        so_phim = n,
                        so_user = n,
                        type = TypeSolution.ELLIPTIC,
                        thuoc_tinh = new List<int> { _curve_property },
                    };
                    log_pharse_3.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_3);
                    WriteFile(_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                    GenericMethod.Clear(concurrent_1);
                }


                #endregion

                #region Pha 4 Trích xuất kết quả Máy chủ thực hiện

                Point[] Aj = new Point[ns];
                if (_run_phase_4)
                {
                    sw.Reset();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đang trích xuất dữ liệu...", DateTime.Now.ToLongTimeString()) });
                    sw.Start();
                    string[] data_phase3 = ReadFileAsLine(_encrypt);
                    Parallel.ForEach(data_phase3, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            AUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                        }
                    });

                    Parallel.For(0, ns, (j) =>
                    {
                        Aj[j] = new Point(0, 0, 1);
                        for (int i = 0; i < n; i++)
                        {
                            Point tmp = Aj[j];
                            Aj[j] = EcdsaMath.Add(tmp, AUij[i, j], _curve.A, _curve.P);
                        }
                        concurrent_1.Add(string.Format("{0},{1},{2}", j, Aj[j].x, Aj[j].y));
                    });
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đã trích xuất kết quả - {1} giây", DateTime.Now.ToLongTimeString(), (double)sw.ElapsedMilliseconds / 1000) });
                    LogInfo log_pharse_4 = new LogInfo()
                    {
                        thoi_gian = sw.ElapsedMilliseconds,
                        pharse = ECCPhase.PHASE_4,
                        so_phim = n,
                        so_user = n
                    };
                    log_pharse_4.SetMetadata();
                    success = LoggerRepository.Instance.Index(log_pharse_4);
                    WriteFile(_sum_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                    GenericMethod.Clear(concurrent_1);
                }
                if (_run_export_sum)
                {
                    sw.Reset();
                    sw.Start();
                    string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                    Parallel.ForEach(data_phase4, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                        }
                    });
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đang vét cạn tính sum", DateTime.Now.ToLongTimeString()) });
                    List<int> data_loga = BRF(Aj, _curve, ns, max * max * n);
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Join(";", data_loga) });
                    WriteFile(_get_sum_encrypt, string.Join(Environment.NewLine, data_loga), false);
                    sw.Stop();
                    UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Vét cạn completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }

            #endregion
        }

        public static List<int> BRF(Point[] Aj, CurveFp curve, int ns, int max)
        {
            List<int> result = new List<int>();
            Point K = new Point(0, 0, 1);
            int count = 0;
            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < ns; j++)
                {
                    if (K == Aj[j])
                    {
                        result.Add(i);
                        count += 1;
                        if (count == ns - 1)
                        {
                            break;
                        }
                    }
                }
                if (count == ns - 1)
                {
                    break;
                }
                K = EcdsaMath.Add(K, curve.G, curve.A, curve.P);
            }

            return result;
        }


        public static void Sim(int[] sum, int m)
        {
            double[] R = new double[m];
            double[,] sim = new double[m, m];
            Parallel.For(0, m, i =>
            {
                R[i] = sum[i] / sum[i + m];
            });

            int l = 0;
            for (int j = 0; j < m - 1; j++)
            {
                for (int k = j + 1; k < m; k++)
                {
                    sim[j, k] = sum[3 * m + l] / (Math.Sqrt(sum[2 * m + j]) * Math.Sqrt(sum[2 * m + k]));
                    l++;
                }
            }
        }




        public static void RunWithoutStopWatch()
        {
            try
            {
                bool append_data = false;
                int so_user = 943;
                int so_phim = 200;

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

                ConcurrentDictionary<(int, int), (BigInteger, BigInteger, BigInteger)> bag = new ConcurrentDictionary<(int, int), (BigInteger, BigInteger, BigInteger)>();


                BigInteger[,] ksuij = new BigInteger[so_user, nk];
                Point[,] KPUij = new Point[so_user, nk];
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                if (_run_phase_1)
                {
                    Parallel.For(0, so_user, (i) =>
                    {
                        Parallel.For(0, nk, (j) =>
                        {
                            BigInteger secret = RSECC.Utils.Integer.randomBetween(1, _curve.order - 1);
                            Point public_key = EcdsaMath.Multiply(_curve.G, secret, _curve.order, _curve.A, _curve.P);
                            bag.TryAdd((i, j), (secret, public_key.x, public_key.y));
                        });
                    });
                    var ordered = bag.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2).ToList();
                    var prv = ordered.Select(x => string.Format("{0},{1},{2}", x.Key.Item1, x.Key.Item2, x.Value.Item1));
                    var pub = ordered.Select(x => string.Format("{0},{1},{2},{3}", x.Key.Item1, x.Key.Item2, x.Value.Item2, x.Value.Item3));
                    WriteFile(_key_user_prv, string.Join(Environment.NewLine, prv), append_data);
                    WriteFile(_key_user_pub, string.Join(Environment.NewLine, pub), append_data);

                    //for (int i = 0; i < m; i++)
                    //{
                    //    for (int j = 0; j < nk; j++)
                    //    {
                    //        BigInteger secret = RSECC.Utils.Integer.randomBetween(1, _curve.order - 1);
                    //        Point public_key = EcdsaMath.Multiply(_curve.G, secret, _curve.order, _curve.A, _curve.P);
                    //        WriteFile(_key_user_prv, string.Format("{0},{1},{2}", i, j, secret), append_data);
                    //        WriteFile(_key_user_pub, string.Format("{0},{1},{2},{3}", i, j, public_key.x, public_key.y), append_data);
                    //    }
                    //}
                }
                #endregion

                #region Pha 2 Tính các khóa công khai dùng chung Máy chủ thực hiện

                ConcurrentDictionary<int, (BigInteger, BigInteger)> bag2 = new ConcurrentDictionary<int, (BigInteger, BigInteger)>();

                Point[] KPj = new Point[nk];
                if (_run_phase_2)
                {
                    string[] key_user_pub = ReadFileAsLine(_key_user_pub);
                    Parallel.ForEach(key_user_pub, line =>
                    {
                        string[] values = line.Split(',');
                        KPUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                    });


                    Parallel.For(0, nk, (j) =>
                    {
                        KPj[j] = new Point(0, 0, 1);
                        for (int i = 0; i < so_user; i++)
                        {
                            Point temp = KPj[j];
                            KPj[j] = EcdsaMath.Add(temp, KPUij[i, j], _curve.A, _curve.P);
                        }
                        bag2.TryAdd(j, (KPj[j].x, KPj[j].y));
                    });
                    var key_common = bag2.OrderBy(x => x.Key).Select(x => string.Format("{0},{1},{2}", x.Key, x.Value.Item1, x.Value.Item2));
                    WriteFile(_key_common, string.Join(Environment.NewLine, key_common), append_data);
                    //for (int j = 0; j < nk; j++)
                    //{
                    //    KPj[j] = new Point(0, 0, 1);
                    //    for (int i = 0; i < m; i++)
                    //    {
                    //        Point temp = KPj[j];
                    //        KPj[j] = EcdsaMath.Add(temp, KPUij[i, j], _curve.A, _curve.P);
                    //    }
                    //    WriteFile(_key_common, string.Format("{0},{1},{2}", j, KPj[j].x, KPj[j].y), append_data);
                    //}
                }


                #endregion

                #region Pha 3 Gửi dữ liệu Những người dùng Ui thực hiện
                so_user = 20;
                Point[,] AUij = new Point[so_user, ns];
                if (_run_phase_3)
                {
                    string[] shared_key = ReadFileAsLine(_key_common);

                    Parallel.ForEach(shared_key, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            KPj[int.Parse(values[0])] = new Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                        }
                    });

                    string[] key_user_prv = ReadFileAsLine(_key_user_prv);
                    Parallel.ForEach(key_user_prv, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            ksuij[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
                        }
                    });

                    ConcurrentDictionary<(int, int), (BigInteger, BigInteger)> bag_encrypt = new ConcurrentDictionary<(int, int), (BigInteger, BigInteger)>();

                    Parallel.For(0, so_user, (i) =>
                    {
                        int j = 0;
                        for (int t = 0; t < nk - 1; t++)
                        {
                            for (int k = t + 1; k < nk; k++)
                            {
                                Point p1 = EcdsaMath.Multiply(_curve.G, Rns[i, j], _curve.order, _curve.A, _curve.P);
                                Point p2 = EcdsaMath.Multiply(KPj[t], ksuij[i, k], _curve.order, _curve.A, _curve.P);
                                Point p3 = EcdsaMath.Multiply(KPj[k], ksuij[i, t], _curve.order, _curve.A, _curve.P);
                                AUij[i, j] = EcdsaMath.Sub(EcdsaMath.Add(p1, p2, _curve.A, _curve.P), p3, _curve.A,
                        _curve.P);
                                bag_encrypt.TryAdd((i, j), (AUij[i, j].x, AUij[i, j].y));
                                if (j == ns - 1) break;
                                else j++;
                            }
                            if (j == ns - 1) break;
                        }
                    });
                    var encrypt = bag_encrypt.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2).Select(x => string.Format("{0},{1},{2},{3}", x.Key.Item1, x.Key.Item2, x.Value.Item1, x.Value.Item2));
                    WriteFile(_encrypt, string.Join(Environment.NewLine,encrypt),false);
                }


                #endregion

                #region Pha 4 Trích xuất kết quả Máy chủ thực hiện

                Point[] Aj = new Point[ns];
                if (_run_phase_4)
                {
                    string[] encrypt = ReadFileAsLine(_encrypt);
                    Parallel.ForEach(encrypt, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            AUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                        }
                    });
                    ConcurrentDictionary<int, Point> bag_sum_encrypt = new ConcurrentDictionary<int, Point>();
                    Parallel.For(0, ns, (j) =>
                    {
                        Aj[j] = new Point(0, 0, 1);
                        for (int i = 0; i < so_user; i++)
                        {
                            Point tmp = Aj[j];
                            Aj[j] = EcdsaMath.Add(tmp, AUij[i, j], _curve.A, _curve.P);
                            bag_sum_encrypt.TryAdd(j, Aj[i]);
                        }
                    });
                    var sum_encypts = bag_sum_encrypt.OrderBy(x => x.Key).Select(x => string.Format("{0},{1},{2}", x.Key, x.Value.x, x.Value.y));
                    WriteFile(_sum_encrypt, string.Join(Environment.NewLine, sum_encypts), false);
                    //for (int j = 0; j < ns; j++)
                    //{
                    //    Aj[j] = new Point(0, 0, 1);
                    //    for (int i = 0; i < m; i++)
                    //    {
                    //        Point tmp = Aj[j];
                    //        Aj[j] = EcdsaMath.Add(tmp, AUij[i, j], _curve.A, _curve.P);
                    //    }
                    //    WriteFile(_sum_encrypt, string.Format("{0},{1},{2}", j, Aj[j].x, Aj[j].y), append_data);
                    //}
                }

                #endregion
                if (_run_export_sum)
                {
                    string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                    Parallel.ForEach(data_phase4, line =>
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                        }
                    });
                    List<int> data_sum_encrypt = BRF(Aj, _curve, ns, max * max * so_user);
                    Sim(data_sum_encrypt.ToArray(), so_user);
                    WriteFile(_get_sum_encrypt, string.Join(Environment.NewLine, data_sum_encrypt), false);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}