using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using ECCBase16;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using RSECC;

namespace RSService
{
    public static class ReSysUtils
    {
        private static readonly ECCBase16.Curve _curve = new ECCBase16.Curve(ECCBase16.CurveName.secp160k1);
        private static Stopwatch _sw = new Stopwatch();

        private static readonly string _key_user_prv = "0.1.KeyUserPrv.txt";
        private static readonly string _key_user_pub = "0.2.KeyUserPub.txt";
        private static readonly string _key_common = "0.3.KeyCommon.txt";
        private static readonly string _encrypt = "0.4.Encrypt.txt";
        private static readonly string _sum_encrypt = "0.5.SumEncrypt.txt";
        private static readonly string _get_sum_encrypt = "0.6.Sum.txt";

        private static bool _run_phase_1 = true;
        private static bool _run_phase_2 = true;
        private static bool _run_phase_3 = true;
        private static bool _run_phase_4 = true;
        private static bool _run_export_sum = true;

        private static int max = 5;
        public static void Run()
        {
            BasicConfigurator.Configure();
            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

            ConcurrentBag<string> concurrent_test = new ConcurrentBag<string>();
            try
            {
                int n = 5   ;
                int m = 20;
                Stopwatch sw = Stopwatch.StartNew();

                int ns = m * (m + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[n, m];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < m; j++)
                    {
                        Ri[i, j] = 0;
                    }
                }

                string[] data = ReadFileInput("Data.txt");
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
                    for (int j = m; j < 2 * m; j++)
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



                BigInteger[,] ksuij = new BigInteger[n, nk];
                EiSiPoint[,] KPUij = new EiSiPoint[n, nk];

                EiSiPoint G = ECCBase16.AffinePoint.ToEiSiPoint(_curve.G);
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                if (_run_phase_1)
                {
                    // UpdateState?.Invoke(null, new LogEventArgs { message = string.Format("[{0}] Đang chuẩn bị các khóa", DateTime.Now.ToLongTimeString()) });

                    sw.Start();
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < nk; j++)
                        {
                            BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                            ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                            concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                            AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                            concurrent_2.Add(string.Format("{0},{1},{2},{3}", i, j, pub_in_affine.X, pub_in_affine.Y));
                        }
                    }
                    //Parallel.For(0, n, i =>
                    //{
                    //    Parallel.For(0, nk, j =>
                    //    {
                    //        BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                    //        ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                    //        AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                    //        concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                    //        concurrent_2.Add(string.Format("{0},{1},{2},{3}", i, j, pub_in_affine.X, pub_in_affine.Y));
                    //    });
                    //});
                    sw.Stop();

                    WriteFile(_key_user_prv, string.Join(Environment.NewLine, concurrent_1), false);
                    WriteFile(_key_user_pub, string.Join(Environment.NewLine, concurrent_2), false);
                    Clear(concurrent_1);
                    Clear(concurrent_2);
                }
                #endregion

                #region Pha 2 Tính các khóa công khai dùng chung Máy chủ thực hiện

                if (_run_phase_2)
                {
                    try
                    {
                        string[] key_user_pub = ReadFileAsLine(_key_user_pub);
                        Parallel.ForEach(key_user_pub, line =>
                        {
                            string[] values = line.Split(',');
                            KPUij[int.Parse(values[0]), int.Parse(values[1])] = new EiSiPoint(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]), 1, _curve);
                        });

                        sw.Reset();
                        sw.Start();
                        for (int j = 0; j < nk; j++)
                        {
                            EiSiPoint KPj = EiSiPoint.InfinityPoint;
                            for (int i = 0; i < n; i++)
                            {
                                ECCBase16.AffinePoint p1 = EiSiPoint.ToAffine(KPj);
                                ECCBase16.AffinePoint p2 = EiSiPoint.ToAffine(KPUij[i, j]);
                                KPj += KPUij[i, j];
                                ECCBase16.AffinePoint px = EiSiPoint.ToAffine(KPj);
                                concurrent_test.Add(string.Format("({0},{1}) + ({2},{3})=({4},{5})", p1.X, p1.Y, p2.X, p2.Y, px.X, px.Y));
                            }
                            ECCBase16.AffinePoint p = EiSiPoint.ToAffine(KPj);
                            concurrent_1.Add(string.Format("{0},{1},{2}", j, p.X, p.Y));
                        }
                        //Parallel.For(0, nk, j =>
                        //{
                        //    KPj[j] =  EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < n; i++)
                        //    {
                        //        KPj[j] = EiSiPoint.Addition(KPj[j], KPUij[i, j]);
                        //    }
                        //    AffinePoint p = EiSiPoint.ToAffine(KPj[j]);
                        //    concurrent_1.Add(string.Format("{0},{1},{2}", j, p.X, p.Y));
                        //});
                        //sw.Stop();

                        WriteFile(_key_common, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }

                }


                #endregion

                #region Pha 3 Gửi dữ liệu Những người dùng Ui thực hiện
                if (_run_phase_3)
                {
                    try
                    {
                        n = 5;
                        EiSiPoint[] KPj = new EiSiPoint[nk];
                        string[] shared_key = ReadFileAsLine(_key_common);

                        Parallel.ForEach(shared_key, line =>
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string[] values = line.Split(',');
                                KPj[int.Parse(values[0])] = new EiSiPoint(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), 1, _curve);
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

                        Dictionary<int, EiSiPoint> dic_repeated = new Dictionary<int, EiSiPoint>();

                        for (int i = 0; i < n; i++)
                        {
                            int j = 0;
                            for (int t = 0; t < nk - 1; t++)
                            {
                                for (int k = t + 1; k < nk; k++)
                                {
                                    if (!dic_repeated.TryGetValue(Rns[i, j], out EiSiPoint p1))
                                    {
                                        p1 = Rns[i, j] * G;
                                        dic_repeated.Add(Rns[i, j], p1);
                                        concurrent_test.Add(string.Format("{0}*({1})=({2})", Rns[i, j], AffinePoint.ToString(_curve.G), AffinePoint.ToString(EiSiPoint.ToAffine(p1))));
                                    }
                                    ECCBase16.EiSiPoint p2 = ksuij[i, k] * KPj[t];
                                    ECCBase16.EiSiPoint p3 = ksuij[i, t] * KPj[k];
                                    ECCBase16.EiSiPoint p4 = p1 + p2 - p3;
                                    if (p4.IsInfinity() || p4.U == 0)
                                    {
                                        concurrent_test.Add(string.Format("({0})+({1})-({2})=({3})", AffinePoint.ToString(EiSiPoint.ToAffine(p1)), AffinePoint.ToString(EiSiPoint.ToAffine(p2)), AffinePoint.ToString(EiSiPoint.ToAffine(p3)), AffinePoint.ToString(AffinePoint.InfinityPoint)));
                                        //concurrent_test.Add(string.Format("({0})+({1})-({2})=({3})", p1.ToString(), p2.ToString(), p3.ToString(), AffinePoint.ToString(AffinePoint.InfinityPoint)));
                                        concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, p4.Nx, p4.Ny));
                                    }
                                    else
                                    {
                                        ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);
                                        concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, p5.X, p5.Y));
                                    }
                                    if (j == ns - 1) break;
                                    else j++;
                                }
                                if (j == ns - 1) break;
                            }
                        }
                        //Parallel.For(0, n, i =>
                        //{
                        //    int j = 0;
                        //    for (int t = 0; t < nk - 1; t++)
                        //    {
                        //        for (int k = t + 1; k < nk; k++)
                        //        {
                        //            ECCBase16.EiSiPoint p1 = EiSiPoint.Base16Multiplicands(Rns[i, j], G);
                        //            ECCBase16.EiSiPoint p2 = EiSiPoint.Base16Multiplicands(ksuij[i, k], KPj[t]);
                        //            ECCBase16.EiSiPoint p3 = EiSiPoint.Base16Multiplicands(ksuij[i, t], KPj[k]);
                        //            ECCBase16.EiSiPoint p4 = EiSiPoint.Subtract(EiSiPoint.Addition(p1, p1), p3);
                        //            ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);
                        //            concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, p5.X, p5.Y));
                        //            if (j == ns - 1) break;
                        //            else j++;
                        //        }
                        //        if (j == ns - 1) break;
                        //    }
                        //});
                        sw.Stop();

                        WriteFile(_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }

                }

                #endregion

                #region Pha 4 Trích xuất kết quả Máy chủ thực hiện
                ECCBase16.AffinePoint[,] AUij = new ECCBase16.AffinePoint[n, ns];

                if (_run_phase_4)
                {
                    try
                    {
                        sw.Reset();

                        sw.Start();
                        string[] data_phase3 = ReadFileAsLine(_encrypt);
                        Parallel.ForEach(data_phase3, line =>
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string[] values = line.Split(',');
                                AUij[int.Parse(values[0]), int.Parse(values[1])] = new ECCBase16.AffinePoint(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]), _curve);
                            }
                        });
                        for (int j = 0; j < ns; j++)
                        {
                            EiSiPoint Aj = EiSiPoint.InfinityPoint;
                            for (int i = 0; i < n; i++)
                            {
                                AffinePoint tmp = EiSiPoint.ToAffine(Aj);
                                Aj += AffinePoint.ToEiSiPoint(AUij[i, j]);
                                concurrent_test.Add(string.Format("({0})+({1})=({2})", AffinePoint.ToString(tmp), AffinePoint.ToString(AUij[i, j]), AffinePoint.ToString(EiSiPoint.ToAffine(Aj))));
                            }
                            concurrent_1.Add(string.Format("{0},{1},{2},{3}", j, Aj.Nx, Aj.Ny, Aj.U));
                        }
                        //Parallel.For(0, ns, (j) =>
                        //{
                        //    Aj[j] = EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < n; i++)
                        //    {
                        //        //EiSiPoint tmp = EiSiPoint.Addition(Aj[j], AffinePoint.ToEiSiPoint(AUij[i, j]));
                        //        //Aj[j] = tmp;
                        //        Aj[j] = Aj[j] + AffinePoint.ToEiSiPoint(AUij[i, j]);
                        //    }
                        //    concurrent_1.Add(string.Format("{0},{1},{2},{3}", j, Aj[j].Nx, Aj[j].Ny, Aj[j].U));
                        //});
                        sw.Stop();

                        WriteFile(_sum_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }

                }
                if (_run_export_sum)
                {
                    try
                    {
                        ECCBase16.EiSiPoint[] Aj = new ECCBase16.EiSiPoint[ns];
                        sw.Reset();
                        sw.Start();
                        string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                        Parallel.ForEach(data_phase4, line =>
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new EiSiPoint(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), BigInteger.Parse(values[3]), _curve);
                        });

                        var data_loga = BRF(Aj, _curve, ns, max * max * n);
                        Sim(data_loga, m);
                        WriteFile(_get_sum_encrypt, string.Join(";", data_loga), false);
                        sw.Stop();
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                    
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.StackTrace);
            }
            WriteFile("Log.txt", String.Join(Environment.NewLine, concurrent_test), false);
            #endregion


        }

        public static int[] BRF(ECCBase16.EiSiPoint[] Aj, Curve curve, int ns, int max)
        {
            int[] result = new int[ns];
            ECCBase16.EiSiPoint K = ECCBase16.EiSiPoint.InfinityPoint;
            EiSiPoint G = ECCBase16.AffinePoint.ToEiSiPoint(curve.G);
            int count = 0;
            for (int i = 0; i < max; i++)
            {
                K = i * G;
                for (int j = 0; j < ns; j++)
                {
                    if (/*K.Ny == Aj[j].Ny && K.Nx == Aj[j].Nx && K.U == Aj[j].U*/ K == Aj[j])
                    {
                        result[j]=i;
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
            }

            return result;
        }


        public static void Sim(int[] sum, int m)
        {
            double[] R = new double[m];
            double[,] sim = new double[m, m];
            Parallel.For(0, m, i =>
            {
                if (sum[i + m] == 0)
                {
                    R[i] = 0;
                }
                else
                {
                    R[i] = sum[i] / sum[i + m];
                }
                
            });
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            int l = 0;
            for (int j = 0; j < m - 1; j++)
            {
                for (int k = j + 1; k < m; k++)
                {
                    sim[j, k] = sum[3 * m + l] / (Math.Sqrt(sum[2 * m + j]) * Math.Sqrt(sum[2 * m + k]));
                    bag.Add(String.Format("Sim({0},{1})={2}", j, k, sim[j, k]));
                    l++;
                }
            }
            WriteFile("Sim.txt", String.Join(Environment.NewLine, bag), false);
        }


        private static string _data_folder0 = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "data");

        private static string _input = "D:\\Test\\Input";
        private static string _data_folder = "D:\\Test\\Output";
        private static void WriteFile(string file_name, string content, bool append = true)
        {
            if (!string.IsNullOrWhiteSpace(file_name))
            {
                if (!Directory.Exists(_data_folder))
                {
                    Directory.CreateDirectory(_data_folder);
                }
                string full_path = Path.Combine(_data_folder, file_name);
                if (append)
                {
                    File.AppendAllText(full_path, content + "\n");
                }
                else
                {

                    File.WriteAllText(full_path, content);
                }
            }
        }

        private static string[] ReadFileAsLine(string file_name)
        {
            string full_path = Path.Combine(_data_folder, file_name);
            if (File.Exists(full_path))
            {
                return File.ReadAllLines(full_path);
            }
            return new string[] { };
        }

        private static string[] ReadFileInput(string file_name)
        {
            string full_path = Path.Combine(_input, file_name);
            if (File.Exists(full_path))
            {
                return File.ReadAllLines(full_path);
            }
            return new string[] { };
        }

        public static void Clear<T>(ConcurrentBag<T> concurrentBag)
        {
            while (!concurrentBag.IsEmpty)
            {
                concurrentBag.TryTake(out _);
            }
        }
    }
}
