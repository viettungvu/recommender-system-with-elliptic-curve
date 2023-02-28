using System;
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
        public static void Run()
        {
            try
            {
                bool success = false;
                UpdateState?.Invoke(null, new LogEventArgs { message = "Service starting..." });
                int so_user = 943;
                int so_phim = 200;
                int max = 5;
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
                //List<RawData> data = new List<RawData>();
                Parallel.ForEach(data, line =>
                {
                    string[] values = line.Split(',');
                    Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
                });
                //foreach (string line in data)
                //{
                //    string[] values = line.Split(',');
                //    Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
                //    //data.Add(new RawData
                //    //{
                //    //    muc_tin_thu_j = int.Parse(values[1]) - 1,
                //    //    nguoi_dung_thu_i = int.Parse(values[0]) - 1,
                //    //    xep_hang = int.Parse(values[2]),
                //    //});
                //}

                //bool success=RatingRepository.Instance.IndexMany(data);
                //var dsach_raw_data = RatingRepository.Instance.GetAll();
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


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                #region Pha 1
                UpdateState?.Invoke(null, new LogEventArgs { message = "Phase 1 started" });
                BigInteger[,] ksuij = new BigInteger[so_user, nk];
                Point[,] KPUij = new Point[so_user, nk];
                sw.Start();

                //for (int i = 0; i < so_user; i++)
                //{
                //    for (int j = 0; j < nk; j++)
                //    {
                //        BigInteger secret = RSECC.Utils.Integer.randomBetween(1, _curve.order - 1);
                //        Point pub = EcdsaMath.Multiply(_curve.G, secret, _curve.order, _curve.A, _curve.P);
                //        //sb.AppendLine(string.Format("{0},{1},{2}", i, j, secret));
                //        //sb1.AppendLine(string.Format("{0},{1},{2},{3}", i, j, pub.x, pub.y));
                //        WriteFile(_file_private_key, string.Format("{0},{1},{2}", i, j, secret));
                //        WriteFile(_file_public_key, string.Format("{0},{1},{2},{3}", i, j, pub.x, pub.y));
                //    }
                //}
                List<string> private_keys = new List<string>();
                List<string> public_keys = new List<string>();
                Parallel.For(0, so_user, i =>
                {
                    Parallel.For(0, nk, j =>
                    {
                        BigInteger secret = RSECC.Utils.Integer.randomBetween(1, _curve.order - 1);
                        Point pub = EcdsaMath.Multiply(_curve.G, secret, _curve.order, _curve.A, _curve.P);
                        ksuij[i, j] = secret;
                        KPUij[i, j] = pub;
                        ///private_keys.Add(string.Format("{0},{1},{2}", i, j, secret));
                        //public_keys.Add(string.Format("{0},{1},{2},{3}", i, j, pub.x, pub.y));
                    });
                });
                sw.Stop();
                //WriteFile(_file_private_key, string.Join("\r\n", private_keys), false);
                //WriteFile(_file_public_key, string.Join("\r\n", public_keys), false);
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


                //WriteFile(_file_private_key, sb.ToString(), false);
                //WriteFile(_file_public_key, sb1.ToString(), false);
                sb.Clear();
                sb1.Clear();
                UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Phase 1 completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
                //return;
                #endregion

                #region Pha 2 Máy chủ thực hiện
                UpdateState?.Invoke(null, new LogEventArgs { message = "Phase 2 started" });

                //string[] PubK = ReadFileAsLine(_file_public_key);
                //Parallel.ForEach(PubK, line =>
                //{
                //    if (!string.IsNullOrWhiteSpace(line))
                //    {
                //        string[] values = line.Split(',');
                //        KPUij[int.Parse(values[0]), int.Parse(values[1])] = new Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                //    }
                //});

                Point[] KPj = new Point[nk];
                sw.Start();
                Parallel.For(0, nk, j =>
                {
                    KPj[j] = new Point(0, 0);
                    ParallelLoopResult res = Parallel.For(0, so_user, i =>
                     {
                         KPj[j] = EcdsaMath.Add(KPj[j], KPUij[i, j], _curve.A, _curve.P);
                     });
                    //if (res.IsCompleted)
                    //{
                    //    sb.AppendLine(string.Format("{0},{1},{2}", j, KPj[j].x, KPj[j].y));
                    //}
                });
                sw.Stop();
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


               /// WriteFile(_file_share_key, sb.ToString(), false);
                sb.Clear();

                UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Phase 2 completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
                #endregion

                #region Pha 3
                UpdateState?.Invoke(null, new LogEventArgs { message = "Phase 3 started" });
                //string[] shared_key = ReadFileAsLine(_file_share_key);

                //Parallel.ForEach(shared_key, line =>
                //{
                //    if (!string.IsNullOrWhiteSpace(line))
                //    {
                //        string[] values = line.Split(',');
                //        KPj[int.Parse(values[0])] = new Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                //    }
                //});

                //string[] data_pha1 = ReadFileAsLine(_file_private_key);
                //Parallel.ForEach(data_pha1, line =>
                //{
                //    if (!string.IsNullOrWhiteSpace(line))
                //    {
                //        string[] values = line.Split(',');
                //        ksuij[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
                //    }
                //});
                ns = 10;
                Point[,] AUij = new Point[so_user, ns];
                sw.Start();
                Parallel.For(0, so_user, i =>
                {
                    int j = 0;
                    Parallel.For(0, nk, t =>
                    {
                        Parallel.For(t + 1, nk, (k, loopState) =>
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
                            if (j == ns - 1) loopState.Stop();
                            else j++;
                        });
                    });
                    sb.AppendLine(string.Format("{0},{1},{2},{3}", i, j, AUij[i, j].x, AUij[i, j].y));
                });
                sw.Stop();
                LogInfo log_pharse_3 = new LogInfo()
                {
                    thoi_gian = sw.ElapsedMilliseconds,
                    pharse = ECCPhase.PHASE_3,
                    so_phim = so_phim,
                    so_user = so_user,
                    type = TypeSolution.ELLIPTIC,
                    thuoc_tinh = new List<int> { _curve_property },
                };
                success = LoggerRepository.Instance.Index(log_pharse_3);
                WriteFile(_file_c_to_server, sb.ToString(), false);
                sb.Clear();
                UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("Phase 3 completed in {0} miliseconds.", sw.ElapsedMilliseconds) });
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
                //    reader1 = new StreamReader(@"0. ECCPhase 3.txt");
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
                //    pharse = ECCPhase.PHARSE_4,
                //    so_phim = so_phim,
                //    so_user = so_user
                //};
                //LoggerRepository.Instance.Index(log_pharse_4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                //Console.WriteLine(ex.Message);
            }

            #endregion
        }

    }


}