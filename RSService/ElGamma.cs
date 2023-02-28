using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RSService
{
    public class ElGamma : BaseRSService
    {

        public static void Run()
        {
            //-------------------------------------------The proposed protocol-------------------------------------------
            //* Prepare the parameters
            BigInteger q, g;//RFC 5114
            BigInteger p = BigInteger.Parse("0b10b8f96a080e01dde92de5eae5d54ec52c99fbcfb06a3c69a6a9dca52d23b616073e28675a23d189838ef1e2ee652c013ecb4aea906112324975c3cd49b83bfaccbdd7d90c4bd7098488e9c219a73724effd6fae5644738faa31a4ff55bccc0a151af5f0dc8b4bd45bf37df365c1a65e68cfda76d4da708df1fb2bc2e4a4371", NumberStyles.AllowHexSpecifier);
            q = BigInteger.Parse("0F518AA8781A8DF278ABA4E7D64B7CB9D49462353", NumberStyles.AllowHexSpecifier);
            g = BigInteger.Parse("0A4D1CBD5C3FD34126765A442EFB99905F8104DD258AC507FD6406CFF14266D31266FEA1E5C41564B777E690F5504F213160217B4B01B886A5E91547F9E2749F4D7FBD7D3B9A92EE1909D0D2263F80A76A6A24C087A091F531DBF0A0169B6A28AD662A4D18E73AFA32D779D5918D08BC8858F4DCEF97C2A24855E6EEB22B3B2E5", NumberStyles.AllowHexSpecifier);
            int n = 943; // number of users
            int m = 200, i, j;         // numver of movies
            int max = 5;
            Random random = new Random();

            Stopwatch sw;
            byte[] array = q.ToByteArray();
            int ns = m * (m + 5) / 2;
            int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
            int[,] Ri = new int[n, 200];
            for (i = 0; i < n; i++)
                for (j = 0; j < m; j++)
                    Ri[i, j] = 0;
            string[] data = ReadFileAsLine(Path.Combine(_data_folder, "Data2.200.txt"));
            j = 0;
            Parallel.ForEach(data, line =>
            {
                var values = line.Split(',');
                Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
            });
            int[,] Rns = new int[n, ns];
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < m; j++)
                    Rns[i, j] = Ri[i, j];
                for (j = m; j < 2 * m; j++)
                {
                    if (Ri[i, j - m] == 0) Rns[i, j] = 0;
                    else Rns[i, j] = 1;
                }
                for (j = 2 * m; j < 3 * m; j++)
                    Rns[i, j] = Ri[i, j - 2 * m] * Ri[i, j - 2 * m];
                for (int t2 = 0; t2 < m - 1; t2++)
                    for (int t22 = t2 + 1; t22 < m; t22++)
                    {
                        Rns[i, j] = Ri[i, t2] * Ri[i, t22];
                        j++;
                    }

            }
            // phase 1            
            BigInteger[,] xi = new BigInteger[n, nk];
            BigInteger[,] Xi = new BigInteger[n, nk];
            string[] phase1 = ReadFileAsLine(Path.Combine(_data_folder, "0.Phase1.txt"));
            Parallel.ForEach(data, line =>
            {
                var values = line.Split(',');
                xi[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
                Xi[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[3]);
            });

            /*BigInteger tg;
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "Phase 1,Sim,"+nk+ ",");
            for (i=0;i<5;i++)//thay n de tinh
            {
                sw = Stopwatch.StartNew();
                sw.Start();
                for (j=0;j<nk;j++)
                {
                    xi[i, j] = 0;
                    while (xi[i, j] <= 0 || xi[i, j] >= q)
                    {
                        random.NextBytes(array);
                        xi[i, j] = new BigInteger(array);
                    }
                    Xi[i,j] = BigInteger.ModPow(g, xi[i,j], p);  // public key
                    //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase1.txt", i+","+j+"," + xi[i,j] + "," + Xi[i, j] + "\n");
                    //TB
                    tg = BigInteger.ModPow(g, xi[i, j], p);
                    tg = BigInteger.ModPow(g, Rns[i, j]+ xi[i, j], p)*BigInteger.ModPow(Xi[i,j],xi[i,j],p)%p;
                }
                sw.Stop();
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", sw.ElapsedMilliseconds+ ",");
            }
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "\n");*/
            //Phase 2            
            BigInteger[] X = new BigInteger[nk];

            string[] phase2 = ReadFileAsLine(Path.Combine(_data_folder, "0.Phase2.txt"));
            Parallel.ForEach(phase2, line =>
            {
                var values = line.Split(',');
                X[int.Parse(values[0])] = BigInteger.Parse(values[1]);
            });
            /*File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Time.txt", "Phase 2," + m1 + "," + nk + ",");
            for (int i1=0;i1<5;i1++)
            {
                sw = Stopwatch.StartNew();
                sw.Start();
                for (j = 0; j < nk; j++)
                {
                    X[j] = 1;
                    for (i = 0; i < n; i++)
                        X[j] = (X[j] * Xi[i, j]) % p;
                    //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase2.txt", j + "," + X[j] + "\n");
                }
                sw.Stop();
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Time.txt", sw.ElapsedMilliseconds + ",");
            }*/
            /*m = 40;
            ns = m *2;
            int nk1   = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));            
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "Phase 4,TB," + m + ","+ns+",");
            for (int i1=0;i1<5;i1++)
            {
                sw = Stopwatch.StartNew();
                sw.Start();                
                for (j = 0; j < ns; j++)
                {
                    X[j%nk] = 1;
                    for (i = 0; i < n; i++)
                        X[j%nk] = (X[j%nk] * Xi[i, j%nk]) % p;
                    //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase2.txt", j + "," + X[j] + "\n");
                    X[j % nk] = 1;
                    for (i = 0; i < n; i++)
                        X[j % nk] = (X[j % nk] * Xi[i, j % nk]) % p;
                    xi[0,j % nk]= (xi[0, j % nk]*BigInteger.ModPow(X[j % nk],p-2,p))% p;
                }               
                sw.Stop();
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", sw.ElapsedMilliseconds + ",");
            }
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "\n"); 
            //phase 3*/
            m = 40;
            ns = m * (m + 5) / 2;
            int nk1 = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "Phase 3,New," + m + ",");
            int t, k;
            BigInteger[,] AU = new BigInteger[n, ns];
            for (i = 0; i < 5; i++)
            {
                sw = Stopwatch.StartNew();
                sw.Start();
                j = 0;
                /*for (t = 0; t < nk1; t++)
                {
                    AU[i, j%nk] = (BigInteger.ModPow(g, xi[i, j%nk], p) * BigInteger.ModPow(X[t%nk], xi[i,j%nk], p)) % p;
                    //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 3.txt", i + "," + j + "," + AU[i, j] + "\n");
                    if (j == nk1 - 1) break;
                    else j++;

                }*/
                for (t = 0; t < nk1 - 1; t++)
                {
                    for (k = t + 1; k < nk1; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(X[t], xi[i, k], p) * modInverse(BigInteger.ModPow(X[k], xi[i, t], p), p)) % p;
                        //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 3.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns) break;
                        else j++;

                    }
                    if (j == ns) break;
                    //else j++;
                }
                sw.Stop();
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", sw.ElapsedMilliseconds + ",");
            }
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "\n");
            int m1 = 0;
            /*for (i = 0; i < 2; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(X[t], xi[i, k], p) * modInverse(BigInteger.ModPow(X[k], xi[i, t], p), p)) % p;
                        //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 31.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns-1) break;
                        else j++;

                    }
                    if (j == ns-1) break;
                    else j++;
                }
            }
            for (; i < n; i++)
            {
                for (; j < ns; j++)
                    AU[i, j] = AU[i, j%50];
            }*/
            /*for (i = 100; i < 200; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 32.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns-1) break;
                        else j++;

                    }
                    if (j == ns-1) break;
                    else j++;
                }
            }
            for (i = 200; i < 300; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 33.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns-1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }
            for (i = 300; i < 400; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 34.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns - 1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }
            for (i = 400; i < 500; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 35.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns - 1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }
            for (i = 500; i < 600; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 36.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns - 1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }
            for (i = 600; i < 700; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 37.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns - 1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }
            for (i = 700; i < 800; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 38.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns - 1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }
            for (i = 800; i < n; i++)
            {
                j = 0;
                for (t = 0; t < nk - 1; t++)
                {
                    for (k = t + 1; k < nk; k++)
                    {
                        AU[i, j] = (BigInteger.ModPow(g, Rns[i, j], p) * BigInteger.ModPow(Xi[i, t], xi[i, k], p) * modInverse(BigInteger.ModPow(Xi[i, k], xi[i, t], p), p)) % p;
                        File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 39.txt", i + "," + j + "," + AU[i, j] + "\n");
                        if (j == ns - 1) break;
                        else j++;

                    }
                    if (j == ns - 1) break;
                    else j++;
                }
            }*/

            //phase 4
            /*reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 3.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 31.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 32.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 33.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 34.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 35.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 36.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 37.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 38.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();
            reader1 = new StreamReader(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0. Phase 39.txt");
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine();
                var values = line.Split(',');
                AU[int.Parse(values[0]), int.Parse(values[1])] = BigInteger.Parse(values[2]);
            }
            reader1.Close();*/
            //int m1 = 40;
            ns = m1 * (m1 + 5) / 2;
            BigInteger[] A = new BigInteger[ns];
            string[] phase4 = ReadFileAsLine(Path.Combine(_data_folder, "0. Phase 4. Ciphertext.txt"));
            j = 0;
            Parallel.ForEach(phase2, (line, loopState) =>
            {
                var values = line.Split(',');
                //TB
                /*if (int.Parse(values[0]) < m1)
                {
                    A[j] = BigInteger.Parse(values[1]);
                    j++;
                }
                else if (int.Parse(values[0]) > m)
                {
                    A[j] = BigInteger.Parse(values[1]);
                    j++;
                    if (j == ns) break;
                }
                //Sim
                if (int.Parse(values[0]) >= 3 * m)
                {
                    A[j] = BigInteger.Parse(values[1]);
                    j++;
                    if (j == ns) break;
                }*/
                if (int.Parse(values[0]) < m1)
                {
                    A[j] = BigInteger.Parse(values[1]);
                    j++;
                }
                else
                {
                    if (int.Parse(values[0]) >= 3 * m)
                    {
                        A[j] = BigInteger.Parse(values[1]);
                        j++;
                        if (j == ns) loopState.Break();
                    }
                    else if ((int.Parse(values[0]) >= 2 * m) && (int.Parse(values[0]) < 2 * m + m1))
                    {
                        A[j] = BigInteger.Parse(values[1]);
                        j++;
                    }
                }
            });
            /* File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "Phase 4,Sim,"+m1+ ","+ns+",");
             for (int i1=0;i1<5;i1++)
             {
                 sw = Stopwatch.StartNew();
                 sw.Start();
                 for (j = 0; j < ns; j++)
                 {
                     A[j] = 1;
                     for (i = 0; i < n; i++)
                         A[j] = (A[j] * AU[i, j]) % p;
                     A[ns - j-1] = 1;
                     for (i = 0; i < n; i++)
                         A[ns-j-1] = A[ns-j-1] * AU[i, ns - j - 1] % p;
                     A[j] = (A[j] * BigInteger.ModPow(A[ns-j-1],p-2,p))%p;
                     //File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4.txt",  j + ","+ A[j] + "\n");
                 }
                 sw.Stop();
                 File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", sw.ElapsedMilliseconds + ",");
             }
             File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "\n");*/
            //BF
            int[] kq = new int[ns];
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "Phase 41,New," + m1 + ",");
            for (i = 0; i < 5; i++)
            {
                sw = new Stopwatch();
                sw.Start();
                kq = BRF(A, g, p, ns, max * max * n);
                sw.Stop();
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", sw.ElapsedMilliseconds + ",");
            }
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.TimeOTB.txt", "\n");
            for (j = 0; j < m; j++)
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4. plaintext.txt", j + "," + kq[j] + "\n");
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4. plaintext.txt", "\n");
            for (; j < 2 * m; j++)
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4. plaintext.txt", j + "," + kq[j] + "\n");
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4. plaintext.txt", "\n");
            for (; j < ns; j++)
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4. plaintext.txt", j + "," + kq[j] + "\n");
            File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.Phase 4. plaintext.txt", "\n");
            //Computing plaintext
            /*for (j=0;j<m;j++)
                File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.KQ.txt", "R[" + j + "]=" + (double)kq[j] / kq[j + m] + "\n");
            t = 0;
            for (j=0;j<m - 1;j++)
                for (k=j + 1; k<m;k++)
                {
                    File.AppendAllText(@"E:\OneDrive - actvn.edu.vn\1. NCS\5. CT_NCS\7.1. OurSolution - RS\0.KQ.txt", "R["+j+","+k+"]="+ kq[3*m+t] / (Math.Sqrt((double)kq[j + 2*m])* Math.Sqrt((double)kq[k + 2 * m])) + "\n");
                    t++;
                }*/
            j = 1;
        }

        private static BigInteger modInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1, x2 = 1, x1 = 0, y2 = 0, y1 = 1, N, r, x, q, y;

            N = n;
            while (n > 0)
            {
                q = BigInteger.Divide(a, n);
                r = BigInteger.Remainder(a, n);
                x = BigInteger.Subtract(x2, BigInteger.Multiply(q, x1));
                y = BigInteger.Subtract(y2, BigInteger.Multiply(q, y1));
                a = n; n = r; x2 = x1; x1 = x; y2 = y1; y1 = y;
            }
            d = a; x = x2; y = y2;
            if (x >= 0)
                return x;
            else
                return BigInteger.Add(x, N);
        }
        private static int[] BRF(BigInteger[] xSj, BigInteger g, BigInteger p, int ns, int max)
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
        }
    }
}
