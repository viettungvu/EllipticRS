using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Transactions;
using ECCBase16;
using ECCJacobian;
using ECCStandard;
using EllipticES;
using EllipticModels;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace RSService
{
    public static class ReSysUtils
    {
        private static readonly ECCBase16.Curve _curve = new ECCBase16.Curve(ECCBase16.CurveName.secp160k1);
        private static readonly CurveFp _curve_jacobian = Curves.getCurveByType(CurveType.sec160k1);
        private static readonly ECCStandard.Curve _curve_standard = new ECCStandard.Curve(ECCStandard.Curve.CurveName.secp160k1);
        private static Stopwatch _sw = new Stopwatch();

        private static readonly string _key_user_prv = "0.1.KeyUserPrv.txt";
        private static readonly string _key_user_pub = "0.2.KeyUserPub.txt";
        private static readonly string _key_common = "0.3.KeyCommon.txt";
        private static readonly string _encrypt = "0.4.Encrypt.txt";
        private static readonly string _sum_encrypt = "0.5.SumEncrypt.txt";
        private static readonly string _sum_encrypt_2 = "0.5.1.SumEncrypt.txt";
        private static readonly string _get_sum_encrypt = "0.6.Sum.txt";
        private static readonly string _sim = "0.7.Sim.txt";
        private static readonly string _sim_round = "0.7.SimRounded.txt";
        private static readonly string _rate_avg = "0.8.RateAvg.txt";
        private static readonly string _rns = "0.0.Rns.txt";





        #region CF
        private static readonly string _cf_key_user_prv = "0.1.RunCF.KeyUserPrv.txt";
        private static readonly string _cf_key_user_pub = "0.1.RunCF.KeyUserPub.txt";

        private static readonly string _cf_cipher_user_part_1 = "0.2.RunCF.CipherTextUserPart1.txt";
        private static readonly string _cf_cipher_user_part_2 = "0.2.RunCF.CipherTextUserPart2.txt";
        #endregion

        private static bool _run_phase_1 = true;
        private static bool _run_phase_2 = true;
        private static bool _run_phase_3 = true;
        private static bool _run_phase_4 = true;
        private static bool _run_export_sum = true;

        private static int max = 5;
        //private static int users = 943;
        //private static int muc_tin = 200;


        private static int users = 5;
        private static int muc_tin = 40;
        private static int ns = muc_tin * (muc_tin + 5) / 2;
        private static int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));

        public static int[] BRFStandard(ECCBase16.AffinePoint[] Aj, int ns, int max)
        {
            int[] result = new int[ns];
            AffinePoint K_sum = AffinePoint.InfinityPoint;
            for (int i = 0; i <= max; i++)
            {
                AffinePoint tmp = K_sum;
                K_sum = AffinePoint.Addition(tmp, _curve.G);
                //ECCBase16.AffinePoint K_mul = ECCBase16.AffinePoint.InfinityPoint;
                //K_mul = AffinePoint.Multiply(i + 1, _curve.G);
                for (int j = 0; j < ns; j++)
                {
                    if (K_sum.X == Aj[j].X && K_sum.Y == Aj[j].Y)
                    {
                        result[j] = i + 1;
                    }
                }
            }
            return result;
        }
        public static int[] BRFEiSi(ECCBase16.EiSiPoint[] Aj, ECCBase16.Curve curve, int ns, int max)
        {
            int[] result = new int[ns];

            ECCBase16.EiSiPoint K = ECCBase16.EiSiPoint.InfinityPoint;
            EiSiPoint G = AffinePoint.ToEiSiPoint(curve.G);
            int count = 0;
            for (int i = 0; i < max; i++)
            {
                K = ECCBase16.EiSiPoint.Multiply(i, G);
                for (int j = 0; j < ns; j++)
                {
                    if (K.Nx == Aj[j].Nx && K.Ny == Aj[j].Ny)
                    {
                        result[j] = i;
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

        public static int[] BRFJacobian(ECCJacobian.Point[] Aj, CurveFp curve, int ns, int max)
        {
            int[] result = new int[ns];
            ECCJacobian.Point K = ECCJacobian.Point.InfinityPoint;

            int count = 0;
            for (int i = 0; i < max; i++)
            {
                K = EcdsaMath.JacobianAdd(curve.G, K, curve.A, curve.P);
                for (int j = 0; j < ns; j++)
                {
                    if (K.x == Aj[j].x && K.y == Aj[j].y && K.z == Aj[j].z)
                    {
                        result[j] = i;
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
        public static int[] BRFStandard(ECCStandard.Point[] Aj, ECCStandard.Curve curve, int ns, int max)
        {
            int[] result = new int[ns];
            ECCStandard.Point K = ECCStandard.Point.InfinityPoint;

            for (int i = 0; i < max; i++)
            {
                ECCStandard.Point tmp = K;
                K = ECCStandard.Point.Add(tmp, curve.G);
                for (int j = 0; j < ns; j++)
                {
                    if (K.X == Aj[j].X && K.Y == Aj[j].Y)
                    {
                        result[j] = i + 1;
                    }
                }
            }
            return result;
        }

        public static void Sim(int[] sum, int muc_tin)
        {
            double[] R = new double[muc_tin];
            double[,] sim = new double[muc_tin, muc_tin];
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            ConcurrentBag<string> bag_round = new ConcurrentBag<string>();
            Parallel.For(0, muc_tin, i =>
            {
                if (sum[i + muc_tin] == 0)
                {
                    R[i] = 0;
                }
                else
                {
                    R[i] = (double)sum[i] / sum[i + muc_tin];
                }
                bag.Add(string.Format("{0},{1}", i, R[i]));
            });
            WriteFile(_rate_avg, String.Join(Environment.NewLine, bag), false);
            Clear(bag);
            int l = 0;
            try
            {
                for (int j = 0; j < muc_tin - 1; j++)
                {
                    for (int k = j + 1; k < muc_tin; k++)
                    {
                        sim[j, k] = sum[3 * muc_tin + l] / (Math.Sqrt(sum[2 * muc_tin + j]) * Math.Sqrt(sum[2 * muc_tin + k]));
                        l++;
                        bag.Add(String.Format("{0},{1},{2}", j, k, sim[j, k]));
                        bag_round.Add(string.Format("{0},{1},{2}", j, k, (int)(sim[j, k] * 100)));
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            WriteFile(_sim, String.Join(Environment.NewLine, bag), false);
            WriteFile(_sim_round, String.Join(Environment.NewLine, bag_round), false);
        }

        public static void RunEiSi(string folder = "")
        {
            _data_folder = folder;
            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

            ConcurrentBag<string> concurrent_test = new ConcurrentBag<string>();
            try
            {
                Stopwatch sw = Stopwatch.StartNew();


                int[,] Ri = new int[users, muc_tin];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < muc_tin; j++)
                    {
                        Ri[i, j] = 0;
                    }
                }

                //string[] data = ReadFileInput("Data2.200.txt");
                string[] data = ReadFileInput("Data.txt");
                List<UserRate> rates = new List<UserRate>();
                Parallel.ForEach(data, line =>
                {
                    string[] values = line.Split(',');
                    Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
                    UserRate rate = new UserRate()
                    {
                        user_id = (int.Parse(values[0]) - 1).ToString(),
                        movie_id = (int.Parse(values[1]) - 1).ToString(),
                        rate= int.Parse(values[2]),
                    };
                    rate.AutoId().SetMetaData();
                    rates.Add(rate);
                });

                UserRateRepository.Instance.IndexMany(rates);
                ConcurrentBag<string> bag_rns = new ConcurrentBag<string>();

                int[,] Rns = new int[users, ns];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < muc_tin; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                        bag_rns.Add(string.Format("{0},{1},{2}", i, j, Rns[i, j]));
                    }
                    for (int j = muc_tin; j < 2 * muc_tin; j++)
                    {
                        if (Ri[i, j - muc_tin] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                        bag_rns.Add(string.Format("{0},{1},{2}", i, j, Rns[i, j]));
                    }
                    for (int j = 2 * muc_tin; j < 3 * muc_tin; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * muc_tin] * Ri[i, j - 2 * muc_tin];
                        bag_rns.Add(string.Format("{0},{1},{2}", i, j, Rns[i, j]));
                    }

                    int t = 3 * muc_tin;
                    for (int t2 = 0; t2 < muc_tin - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < muc_tin; t22++)
                        {
                            Rns[i, t] = Ri[i, t2] * Ri[i, t22];
                            bag_rns.Add(string.Format("{0},{1},{2}", i, t, Rns[i, t]));
                            t++;
                        }
                    }
                }

                WriteFile(_rns, string.Join(Environment.NewLine, bag_rns), false);

                BigInteger[,] ksuij = new BigInteger[users, nk];
                EiSiPoint[,] KPUij = new EiSiPoint[users, nk];
                EiSiPoint G = ECCBase16.AffinePoint.ToEiSiPoint(_curve.G);
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                if (_run_phase_1)
                {
                    sw.Start();
                    try
                    {
                        ConcurrentBag<PharseContent> list = new ConcurrentBag<PharseContent>();
                        Parallel.For(0, users, (i) =>
                        {
                            Parallel.For(0, nk, (j) =>
                            {
                                BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                                ksuij[i, j] = secret;
                                ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                                concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                                AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                                concurrent_2.Add(string.Format("{0},{1},{2}", i, j, pub_in_affine.ToString()));

                                PharseContent content = new PharseContent()
                                {
                                    user_id = i.ToString(),
                                    key_index = j,
                                    secret = secret.ToString(),
                                    point = PointPharseContent.Map(pub_in_affine),
                                    pharse=Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                                    total_movies=muc_tin,
                                    total_users=users,
                                };
                                content.AutoId().SetMetaData();
                                list.Add(content);
                            });
                        });
                        PharseContentRepository.Instance.IndexMany(list);
                        //for (int i = 0; i < users; i++)
                        //{
                        //    for (int j = 0; j < nk; j++)
                        //    {
                        //        BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                        //        ksuij[i, j] = secret;
                        //        ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                        //        concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                        //        AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                        //        concurrent_2.Add(string.Format("{0},{1},{2}", i, j, pub_in_affine.ToString()));
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
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
                        //string[] key_user_pub = ReadFileInput(_key_user_pub);
                        Parallel.ForEach(key_user_pub, line =>
                        {
                            string[] values = line.Split(',');
                            KPUij[int.Parse(values[0]), int.Parse(values[1])] = new EiSiPoint(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]), 1, _curve);
                        });


                        sw.Reset();
                        sw.Start();

                        //for (int j = 0; j < nk; j++)
                        //{
                        //    EiSiPoint KPj = EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < users; i++)
                        //    {
                        //        //ECCBase16.AffinePoint p1 = EiSiPoint.ToAffine(KPj);
                        //        //ECCBase16.AffinePoint p2 = EiSiPoint.ToAffine(KPUij[i, j]);
                        //        EiSiPoint temp = KPj;
                        //        KPj = EiSiPoint.Addition(temp, KPUij[i, j]);
                        //        //ECCBase16.AffinePoint px = EiSiPoint.ToAffine(KPj);
                        //        //concurrent_test.Add(string.Format("({0}) + ({1})=({2})", p1.ToString(), p2.ToString(), px.ToString()));
                        //    }
                        //    ECCBase16.AffinePoint p = EiSiPoint.ToAffine(KPj);
                        //    concurrent_1.Add(string.Format("{0},{1}", j, p.ToString()));
                        //}
                        Parallel.For(0, nk, j =>
                        {
                            EiSiPoint KPj = EiSiPoint.InfinityPoint;
                            for (int i = 0; i < users; i++)
                            {
                                EiSiPoint tmp = KPj;
                                KPj = EiSiPoint.Addition(tmp, KPUij[i, j]);
                            }
                            ECCBase16.AffinePoint p = EiSiPoint.ToAffine(KPj);
                            concurrent_1.Add(string.Format("{0},{1}", j, p.ToString()));
                        });
                        sw.Stop();

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
                        ConcurrentDictionary<int, EiSiPoint> dic_repeated = new ConcurrentDictionary<int, EiSiPoint>();

                        //for (int i = 0; i < users; i++)
                        //{
                        //    int j = 0;
                        //    for (int t = 0; t < nk - 1; t++)
                        //    {
                        //        for (int k = t + 1; k < nk; k++)
                        //        {
                        //            //if (!dic_repeated.TryGetValue(Rns[i, j], out EiSiPoint p1))
                        //            //{
                        //            //    p1 = EiSiPoint.Multiply(Rns[i, j], G);
                        //            //    dic_repeated.TryAdd(Rns[i, j], p1);
                        //            //    //concurrent_test.Add(string.Format("{0}*({1})=({2})", Rns[i, j], _curve.G.ToString(), EiSiPoint.ToAffine(p1).ToString()));
                        //            //}
                        //            EiSiPoint p1 = EiSiPoint.Multiply(Rns[i, j], G);
                        //            ECCBase16.EiSiPoint p2 = EiSiPoint.Base16Multiplicands(ksuij[i, k], KPj[t]);
                        //            ECCBase16.EiSiPoint p3 = EiSiPoint.Base16Multiplicands(ksuij[i, t], KPj[k]);

                        //            //var p1_aff = EiSiPoint.ToAffine(p1);
                        //            //var p2_aff = EiSiPoint.ToAffine(p2);
                        //            //var p3_aff = EiSiPoint.ToAffine(p3);
                        //            var tmp = EiSiPoint.ToAffine(EiSiPoint.Addition(p1, p2));
                        //            ECCBase16.EiSiPoint p4 = EiSiPoint.Subtract(EiSiPoint.Addition(p1, p2), p3);

                        //            ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);
                        //            concurrent_1.Add(string.Format("{0},{1},{2}", i, j, p5.ToString()));
                        //            if (j == ns - 1) break;
                        //            else j++;
                        //        }
                        //        if (j == ns - 1) break;
                        //    }
                        //}
                        Parallel.For(0, users, i =>
                        {
                            int j = 0;
                            for (int t = 0; t < nk - 1; t++)
                            {
                                for (int k = t + 1; k < nk; k++)
                                {
                                    if (!dic_repeated.TryGetValue(Rns[i, j], out EiSiPoint p1))
                                    {
                                        p1 = EiSiPoint.Multiply(Rns[i, j], G);
                                        dic_repeated.TryAdd(Rns[i, j], p1);
                                    }
                                    ECCBase16.EiSiPoint p2 = EiSiPoint.Base16Multiplicands(ksuij[i, k], KPj[t]);
                                    ECCBase16.EiSiPoint p3 = EiSiPoint.Base16Multiplicands(ksuij[i, t], KPj[k]);

                                    var tmp = EiSiPoint.ToAffine(EiSiPoint.Addition(p1, p2));
                                    ECCBase16.EiSiPoint p4 = EiSiPoint.Subtract(EiSiPoint.Addition(p1, p2), p3);

                                    ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);
                                    concurrent_1.Add(string.Format("{0},{1},{2}", i, j, p5.ToString()));
                                    if (j == ns - 1) break;
                                    else j++;
                                }
                                if (j == ns - 1) break;
                            }
                        });
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
                ECCBase16.AffinePoint[,] AUij = new ECCBase16.AffinePoint[users, ns];
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
                        //for (int j = 0; j < ns; j++)
                        //{
                        //    EiSiPoint Aj = EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < users; i++)
                        //    {
                        //        EiSiPoint tmp = Aj;
                        //        Aj = EiSiPoint.Addition(tmp, AffinePoint.ToEiSiPoint(AUij[i, j]));
                        //    }
                        //    AffinePoint affine = EiSiPoint.ToAffine(Aj);
                        //    concurrent_1.Add(string.Format("{0},{1}", j, affine.ToString()));
                        //}
                        Parallel.For(0, ns, (j) =>
                        {
                            EiSiPoint Aj = EiSiPoint.InfinityPoint;
                            for (int i = 0; i < users; i++)
                            {
                                EiSiPoint tmp = Aj;
                                Aj = EiSiPoint.Addition(tmp, AffinePoint.ToEiSiPoint(AUij[i, j]));
                            }
                            AffinePoint affine = EiSiPoint.ToAffine(Aj);
                            concurrent_1.Add(string.Format("{0},{1}", j, affine.ToString()));
                        });
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
                    AffinePoint[] Aj = new AffinePoint[ns];
                    try
                    {
                        sw.Reset();
                        sw.Start();
                        string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                        Parallel.ForEach(data_phase4, line =>
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new AffinePoint(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), _curve);
                        });
                        int[] data_loga = BRFStandard(Aj, ns, max * max * users);
                        WriteFile(_get_sum_encrypt, string.Join(";", data_loga), false);
                        Sim(data_loga, muc_tin);
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

        public static void RunCF()
        {

            int user_target = 0;

            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_3 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_4 = new ConcurrentBag<string>();


            #region Pha 1:User target tạo khóa bí mật và khóa công khai 
            BigInteger xi = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
            EiSiPoint Xi_eisi = EiSiPoint.Base16Multiplicands(xi, AffinePoint.ToEiSiPoint(_curve.G));
            AffinePoint Xi = EiSiPoint.ToAffine(Xi_eisi);


            #endregion


            #region Pha 2: User target mã hóa xếp hạng

            for (int j = 0; j < muc_tin; j++)
            {
                BigInteger cj = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);

                int rate = 0;

                EiSiPoint p1 = EiSiPoint.Base16Multiplicands(rate, AffinePoint.ToEiSiPoint(_curve.G));
                EiSiPoint p2 = EiSiPoint.Base16Multiplicands(cj, AffinePoint.ToEiSiPoint(Xi));
                EiSiPoint C1j = EiSiPoint.Addition(p1, p2);
                EiSiPoint C2j = EiSiPoint.Base16Multiplicands(cj, AffinePoint.ToEiSiPoint(_curve.G));

                AffinePoint C1j_affine = EiSiPoint.ToAffine(C1j);
                AffinePoint C2j_affine = EiSiPoint.ToAffine(C2j);

                concurrent_1.Add(string.Format("{0},{1}", j, C1j_affine.ToString()));
                concurrent_2.Add(string.Format("{0},{1}", j, C2j_affine.ToString()));
            }
            WriteFile(_cf_cipher_user_part_1, string.Join(Environment.NewLine, concurrent_1), false);
            WriteFile(_cf_cipher_user_part_2, string.Join(Environment.NewLine, concurrent_2), false);
            Clear(concurrent_1);
            Clear(concurrent_2);
            #endregion

            #region Pha 2:
            string[] str_sim_round = ReadFileAsLine(_sim_round);
            int[,] sim_rounded = new int[muc_tin, muc_tin];
            Parallel.ForEach(str_sim_round, line =>
            {
                string[] values = line.Split(',');
                sim_rounded[int.Parse(values[0]), int.Parse(values[1])] = int.Parse(values[2]);
            });

            string[] str_rate_avg = ReadFileAsLine(_rate_avg);
            int[] rate_round_avg = new int[muc_tin];

            Parallel.ForEach(str_sim_round, line =>
            {
                string[] values = line.Split(',');
                double.TryParse(values[1], out double rate_avg);
                int avg_rounded = (int)rate_avg;
                rate_round_avg[int.Parse(values[0])] = avg_rounded;
            });

            EiSiPoint[] ctext_part_1 = new EiSiPoint[muc_tin];
            EiSiPoint[] ctext_part_2 = new EiSiPoint[muc_tin];

            string[] data_part_1 = ReadFileAsLine(_cf_cipher_user_part_1);

            Parallel.ForEach(data_part_1, line =>
            {
                string[] values = line.Split(',');
                ctext_part_1[int.Parse(values[0])] = new EiSiPoint(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), 1, _curve);
            });
            string[] data_part_2 = ReadFileAsLine(_cf_cipher_user_part_2);
            Parallel.ForEach(data_part_2, line =>
            {
                string[] values = line.Split(',');
                ctext_part_2[int.Parse(values[0])] = new EiSiPoint(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), 1, _curve);
            });


            EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);
            for (int i = 0; i < muc_tin; i++)
            {
                EiSiPoint sum5 = EiSiPoint.InfinityPoint;
                for (int j = 0; j < muc_tin - 1; j++)
                {
                    for (int k = j + 1; k < muc_tin; k++)
                    {
                        BigInteger c1 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                        BigInteger c2 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                        BigInteger c3 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                        EiSiPoint skj_g = EiSiPoint.Base16Multiplicands(sim_rounded[k, j], G);
                    }
                }

            }




            #region old
            //BigInteger c1 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
            //BigInteger c2 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
            //BigInteger c3 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
            //EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);
            //Tính F5, k





            //EiSiPoint p = EiSiPoint.Base16Multiplicands(c1, AffinePoint.ToEiSiPoint(Xi));

            //EiSiPoint sum5 = EiSiPoint.InfinityPoint;
            //EiSiPoint f7 = EiSiPoint.InfinityPoint;
            //for (int j = 0; j < muc_tin; j++)
            //{
            //    EiSiPoint p5 = EiSiPoint.Base16Multiplicands(sim_rounded[muc_tin_target, j], G);
            //    sum5 = EiSiPoint.Addition(sum5, p5);


            //    EiSiPoint p7 = EiSiPoint.Base16Multiplicands(rate_round_avg[j], G);
            //    EiSiPoint p7j = EiSiPoint.Addition(p7, p);
            //    f7 = EiSiPoint.Addition(f7, p7j);
            //};
            //f7 = EiSiPoint.Addition(f7, EiSiPoint.Base16Multiplicands(c2, AffinePoint.ToEiSiPoint(Xi)));

            //sum5 = EiSiPoint.Base16Multiplicands(rate_round_avg[muc_tin_target], sum5);
            //EiSiPoint f5 = EiSiPoint.Addition(EiSiPoint.Base16Multiplicands(rate_round_avg[muc_tin_target], sum5), p);


            //ECCBase16.EiSiPoint f6 = EiSiPoint.Base16Multiplicands(c1, AffinePoint.ToEiSiPoint(_curve.G));
            //ECCBase16.EiSiPoint f8 = EiSiPoint.Base16Multiplicands(c2, AffinePoint.ToEiSiPoint(_curve.G));
            //ECCBase16.EiSiPoint f12 = EiSiPoint.Base16Multiplicands(c3, AffinePoint.ToEiSiPoint(_curve.G));


            /// Tính f9, f10, f11, f12
            //EiSiPoint f9 = EiSiPoint.InfinityPoint;
            //EiSiPoint f10 = EiSiPoint.InfinityPoint;
            //EiSiPoint f11 = EiSiPoint.InfinityPoint;
            //for (int j = 0; j < muc_tin; j++)
            //{
            //    EiSiPoint sub9 = EiSiPoint.Subtract(ctext_part_1[j], f7);
            //    EiSiPoint mul9 = EiSiPoint.Base16Multiplicands(sim_rounded[muc_tin_target, j], sub9);
            //    EiSiPoint tmp9 = f9;
            //    f9 = EiSiPoint.Addition(tmp9, mul9);

            //    EiSiPoint sub10 = EiSiPoint.Subtract(ctext_part_2[j], f8);
            //    EiSiPoint mul10 = EiSiPoint.Multiply(sim_rounded[muc_tin_target, j], sub10);
            //    EiSiPoint tmp10 = f10;
            //    f10 = EiSiPoint.Addition(tmp10, mul10);

            //    EiSiPoint mul11 = EiSiPoint.Multiply(sim_rounded[muc_tin_target, j], G);
            //    f11 = EiSiPoint.Addition(f11, mul11);
            //}

            //f9 = EiSiPoint.Addition(f5, f9);
            //f10 = EiSiPoint.Addition(f6, f10);
            //f11 = EiSiPoint.Addition(f11, EiSiPoint.Base16Multiplicands(c3, AffinePoint.ToEiSiPoint(Xi)));


            //#endregion


            //#region Pha 4;

            //AffinePoint Ck5 = EiSiPoint.ToAffine(EiSiPoint.Subtract(f9, EiSiPoint.Base16Multiplicands(xi, f10)));
            //AffinePoint C = EiSiPoint.ToAffine(EiSiPoint.Subtract(f11, EiSiPoint.Base16Multiplicands(xi, f12)));


            //int dk = 0;
            //int d = 0;
            //AffinePoint sumDk = AffinePoint.InfinityPoint;
            //AffinePoint sumD = AffinePoint.InfinityPoint;
            //for (int j = 0; j < muc_tin; j++)
            //{
            //    sumDk = AffinePoint.Multiply(j, _curve.G);
            //    sumD = AffinePoint.Multiply(j, _curve.G);

            //    if (sumDk.X == Ck5.X && sumDk.Y == Ck5.Y)
            //    {
            //        dk = j;
            //    }
            //    if (sumD.X == C.X && sumD.Y == C.Y)
            //    {
            //        d = j;
            //    }
            //}

            //double Pik = dk / d;
            #endregion
            #endregion
        }


        public static void RunJacobian(string folder = "")
        {
            _data_folder = folder;
            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

            ConcurrentBag<string> concurrent_test = new ConcurrentBag<string>();
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                int ns = muc_tin * (muc_tin + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[users, muc_tin];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < muc_tin; j++)
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
                int[,] Rns = new int[users, ns];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < muc_tin; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                    }
                    for (int j = muc_tin; j < 2 * muc_tin; j++)
                    {
                        if (Ri[i, j - muc_tin] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                    }
                    for (int j = 2 * muc_tin; j < 3 * muc_tin; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * muc_tin] * Ri[i, j - 2 * muc_tin];
                    }

                    int t = 3 * muc_tin;
                    for (int t2 = 0; t2 < users - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < muc_tin; t22++)
                        {
                            Rns[i, t] = Ri[i, t2] * Ri[i, t22];
                            t++;
                        }
                    }
                }



                BigInteger[,] ksuij = new BigInteger[users, nk];
                ECCJacobian.Point[,] KPUij = new ECCJacobian.Point[users, nk];
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                if (_run_phase_1)
                {

                    sw.Start();
                    for (int i = 0; i < users; i++)
                    {
                        for (int j = 0; j < nk; j++)
                        {
                            BigInteger secret = ECCJacobian.Utils.Integer.randomBetween(1, _curve_jacobian.N - 1);
                            ECCJacobian.Point pub = EcdsaMath.JacobianMultiply(_curve_jacobian.G, secret, _curve_jacobian.N, _curve_jacobian.A, _curve_jacobian.P);
                            concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                            ECCJacobian.Point p = EcdsaMath.FromJacobian(pub, _curve_jacobian.P);
                            concurrent_2.Add(string.Format("{0},{1},{2},{3}", i, j, p.x, p.y));
                        }
                    }
                    //Parallel.For(0, users, i =>
                    //{
                    //    Parallel.For(0, nk, j =>
                    //    {
                    //        BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                    //        ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                    //        AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                    //        concurrent_1.Addition(string.Format("{0},{1},{2}", i, j, secret));
                    //        concurrent_2.Addition(string.Format("{0},{1},{2},{3}", i, j, pub_in_affine.X, pub_in_affine.Y));
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
                            KPUij[int.Parse(values[0]), int.Parse(values[1])] = new ECCJacobian.Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                        });

                        sw.Reset();
                        sw.Start();
                        for (int j = 0; j < nk; j++)
                        {
                            ECCJacobian.Point KPj = ECCJacobian.Point.InfinityPoint;
                            for (int i = 0; i < users; i++)
                            {
                                ECCJacobian.Point p1 = EcdsaMath.FromJacobian(KPj, _curve_jacobian.P);
                                ECCJacobian.Point p2 = KPUij[i, j];
                                KPj = EcdsaMath.JacobianAdd(KPUij[i, j], KPj, _curve_jacobian.A, _curve_jacobian.P);
                                ECCJacobian.Point p3 = EcdsaMath.FromJacobian(KPj, _curve_jacobian.P);
                                concurrent_test.Add(string.Format("({0},{1}) + ({2},{3})=({4},{5})", p1.x, p1.y, p2.x, p2.y, p3.x, p3.y));
                            }
                            ECCJacobian.Point p = EcdsaMath.FromJacobian(KPj, _curve_jacobian.P);
                            concurrent_1.Add(string.Format("{0},{1},{2}", j, p.x, p.y));
                        }
                        //Parallel.For(0, nk, j =>
                        //{
                        //    KPj[j] =  EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < users; i++)
                        //    {
                        //        KPj[j] = EiSiPoint.Addition(KPj[j], KPUij[i, j]);
                        //    }
                        //    AffinePoint p = EiSiPoint.ToAffine(KPj[j]);
                        //    concurrent_1.Addition(string.Format("{0},{1},{2}", j, p.X, p.Y));
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
                        ECCJacobian.Point[] KPj = new ECCJacobian.Point[nk];
                        string[] shared_key = ReadFileInput(_key_common);

                        Parallel.ForEach(shared_key, line =>
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string[] values = line.Split(',');
                                KPj[int.Parse(values[0])] = new ECCJacobian.Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]));
                            }
                        });

                        string[] key_user_prv = ReadFileInput(_key_user_prv);
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

                        Dictionary<int, ECCJacobian.Point> dic_repeated = new Dictionary<int, ECCJacobian.Point>();

                        for (int i = 0; i < users; i++)
                        {
                            int j = 0;
                            for (int t = 0; t < nk - 1; t++)
                            {
                                for (int k = t + 1; k < nk; k++)
                                {
                                    if (!dic_repeated.TryGetValue(Rns[i, j], out ECCJacobian.Point p1))
                                    {
                                        p1 = EcdsaMath.JacobianMultiply(_curve_jacobian.G, Rns[i, j], _curve_jacobian.N, _curve_jacobian.A, _curve_jacobian.P);
                                        dic_repeated.Add(Rns[i, j], p1);
                                        concurrent_test.Add(string.Format("{0}*({1})=({2})", Rns[i, j], _curve_jacobian.G.ToString(), EcdsaMath.FromJacobian(p1, _curve_jacobian.P).ToString()));
                                    }
                                    ECCJacobian.Point p2 = EcdsaMath.JacobianMultiply(KPj[t], ksuij[i, k], _curve_jacobian.N, _curve_jacobian.A, _curve_jacobian.P);
                                    ECCJacobian.Point p3 = EcdsaMath.JacobianMultiply(KPj[k], ksuij[i, t], _curve_jacobian.N, _curve_jacobian.A, _curve_jacobian.P);
                                    ECCJacobian.Point p4 = EcdsaMath.JacobianSub(EcdsaMath.Add(p1, p2, _curve_jacobian
                                        .A, _curve_jacobian.P), p3, _curve_jacobian.A, _curve_jacobian.P);
                                    if (p4.IsInfinity())
                                    {
                                        concurrent_test.Add(string.Format("({0})+({1})-({2})=({3})", EcdsaMath.FromJacobian(p1, _curve_jacobian.P).ToString(), EcdsaMath.FromJacobian(p2, _curve_jacobian.P).ToString(), EcdsaMath.FromJacobian(p3, _curve_jacobian.P).ToString(), EcdsaMath.FromJacobian(p4, _curve_jacobian.P).ToString()));
                                        concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, 0, 0));
                                    }
                                    else
                                    {
                                        ECCJacobian.Point p5 = EcdsaMath.FromJacobian(p4, _curve_jacobian.P);
                                        concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, p5.x, p5.y));
                                    }
                                    if (j == ns - 1) break;
                                    else j++;
                                }
                                if (j == ns - 1) break;
                            }
                        }
                        //Parallel.For(0, users, i =>
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
                        //            concurrent_1.Addition(string.Format("{0},{1},{2},{3}", i, j, p5.X, p5.Y));
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
                ECCJacobian.Point[,] AUij = new ECCJacobian.Point[users, ns];

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
                                AUij[int.Parse(values[0]), int.Parse(values[1])] = new ECCJacobian.Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                            }
                        });
                        for (int j = 0; j < ns; j++)
                        {
                            ECCJacobian.Point Aj = ECCJacobian.Point.InfinityPoint;
                            for (int i = 0; i < users; i++)
                            {
                                try
                                {
                                    ECCJacobian.Point tmp = Aj;
                                    Aj = EcdsaMath.JacobianAdd(AUij[i, j], tmp, _curve_jacobian.A, _curve_jacobian.P);
                                    concurrent_test.Add(string.Format("({0})+({1})=({2})", tmp.ToString(), EcdsaMath.FromJacobian(AUij[i, j], _curve_jacobian.P).ToString(), EcdsaMath.FromJacobian(Aj, _curve_jacobian.P).ToString()));
                                }
                                catch (Exception ex)
                                {
                                    throw;
                                }
                            }
                            concurrent_1.Add(string.Format("{0},{1},{2},{3}", j, Aj.x, Aj.y, Aj.z));
                        }
                        //Parallel.For(0, ns, (j) =>
                        //{
                        //    Aj_affine[j] = EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < users; i++)
                        //    {
                        //        EiSiPoint tmp = EiSiPoint.Addition(Aj_affine[j], AffinePoint.ToEiSiPoint(AUij[i, j]));
                        //        Aj_affine[j] = tmp;
                        //        Aj_affine[j] = Aj_affine[j] + AffinePoint.ToEiSiPoint(AUij[i, j]);
                        //    }
                        //    concurrent_1.Addition(string.Format("{0},{1},{2},{3}", j, Aj_affine[j].Nx, Aj_affine[j].Ny, Aj_affine[j].U));
                        //});
                        sw.Stop();

                        WriteFile(_sum_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                    #endregion
                }
                if (_run_export_sum)
                {
                    try
                    {
                        ECCJacobian.Point[] Aj = new ECCJacobian.Point[ns];
                        sw.Reset();
                        sw.Start();
                        string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                        Parallel.ForEach(data_phase4, line =>
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new ECCJacobian.Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), BigInteger.Parse(values[3]));
                        });

                        var data_loga = BRFJacobian(Aj, _curve_jacobian, ns, max * max * users);
                        Sim(data_loga, muc_tin);
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
        }


        public static void RunStandard(string folder = "")
        {
            _data_folder = folder;
            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

            ConcurrentBag<string> concurrent_test = new ConcurrentBag<string>();
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                int ns = muc_tin * (muc_tin + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[users, muc_tin];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < muc_tin; j++)
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
                int[,] Rns = new int[users, ns];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < muc_tin; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                    }
                    for (int j = muc_tin; j < 2 * muc_tin; j++)
                    {
                        if (Ri[i, j - muc_tin] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                    }
                    for (int j = 2 * muc_tin; j < 3 * muc_tin; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * muc_tin] * Ri[i, j - 2 * muc_tin];
                    }

                    int t = 3 * muc_tin;
                    for (int t2 = 0; t2 < muc_tin - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < muc_tin; t22++)
                        {
                            Rns[i, t] = Ri[i, t2] * Ri[i, t22];
                            t++;
                        }
                    }
                }






                BigInteger[,] ksuij = new BigInteger[users, nk];
                ECCStandard.Point[,] KPUij = new ECCStandard.Point[users, nk];
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                if (_run_phase_1)
                {

                    sw.Start();
                    for (int i = 0; i < users; i++)
                    {
                        for (int j = 0; j < nk; j++)
                        {
                            Cryptography.KeyPair key_pair = Cryptography.GetKeyPair(_curve_standard);
                            concurrent_1.Add(string.Format("{0},{1},{2}", i, j, key_pair.PrivateKey));
                            concurrent_2.Add(string.Format("{0},{1},{2},{3}", i, j, key_pair.PublicKey.X, key_pair.PublicKey.Y));
                            concurrent_test.Add(string.Format("{0}*({1})=({2})", key_pair.PrivateKey, _curve_standard.G.ToString(), key_pair.PublicKey.ToString()));
                        }
                    }
                    //Parallel.For(0, users, i =>
                    //{
                    //    Parallel.For(0, nk, j =>
                    //    {
                    //        BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                    //        ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                    //        AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                    //        concurrent_1.Addition(string.Format("{0},{1},{2}", i, j, secret));
                    //        concurrent_2.Addition(string.Format("{0},{1},{2},{3}", i, j, pub_in_affine.X, pub_in_affine.Y));
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
                            KPUij[int.Parse(values[0]), int.Parse(values[1])] = new ECCStandard.Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]), _curve_standard);
                        });
                    }
                    catch (Exception ex)
                    {
                        concurrent_test.Add(ex.Message);
                    }


                    sw.Reset();
                    sw.Start();
                    for (int j = 0; j < nk; j++)
                    {
                        ECCStandard.Point KPj = ECCStandard.Point.InfinityPoint;
                        for (int i = 0; i < users; i++)
                        {
                            ECCStandard.Point tmp = KPj;
                            KPj = ECCStandard.Point.Add(KPUij[i, j], tmp);
                            //if (ECCStandard.Point.IsInfinityPoint(tmp))
                            //{
                            //    concurrent_test.Addition(string.Format("({0},{1}) + ({2})=({3})", 0, 0, KPUij[i, j].ToString(), KPj.ToString()));
                            //}
                            //else
                            //{
                            //    concurrent_test.Addition(string.Format("({0}) + ({1})=({2})", tmp.ToString(), KPUij[i, j].ToString(), KPj.ToString()));
                            //}
                        }
                        concurrent_1.Add(string.Format("{0},{1},{2}", j, KPj.X, KPj.Y));
                    }
                    //Parallel.For(0, nk, j =>
                    //{
                    //    KPj[j] =  EiSiPoint.InfinityPoint;
                    //    for (int i = 0; i < users; i++)
                    //    {
                    //        KPj[j] = EiSiPoint.Addition(KPj[j], KPUij[i, j]);
                    //    }
                    //    AffinePoint p = EiSiPoint.ToAffine(KPj[j]);
                    //    concurrent_1.Addition(string.Format("{0},{1},{2}", j, p.X, p.Y));
                    //});
                    //sw.Stop();

                    WriteFile(_key_common, string.Join(Environment.NewLine, concurrent_1), false);
                    Clear(concurrent_1);
                }


                #endregion

                #region Pha 3 Gửi dữ liệu Những người dùng Ui thực hiện
                if (_run_phase_3)
                {
                    try
                    {
                        ECCStandard.Point[] KPj = new ECCStandard.Point[nk];
                        string[] shared_key = ReadFileAsLine(_key_common);

                        Parallel.ForEach(shared_key, line =>
                                        {
                                            if (!string.IsNullOrWhiteSpace(line))
                                            {
                                                string[] values = line.Split(',');
                                                KPj[int.Parse(values[0])] = new ECCStandard.Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), _curve_standard);
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


                        for (int i = 0; i < users; i++)
                        {
                            int j = 0;
                            for (int t = 0; t < nk - 1; t++)
                            {
                                for (int k = t + 1; k < nk; k++)
                                {
                                    ECCStandard.Point p1 = ECCStandard.Point.Multiply(Rns[i, j], _curve_standard.G);
                                    ECCStandard.Point p2 = ECCStandard.Point.Multiply(ksuij[i, k], KPj[t]);
                                    ECCStandard.Point p3 = ECCStandard.Point.Multiply(ksuij[i, t], KPj[k]);
                                    ECCStandard.Point p4 = ECCStandard.Point.Add(ECCStandard.Point.Add(p1, p2), ECCStandard.Point.Negate(p3));
                                    concurrent_test.Add(string.Format("({0})+({1})-({2})=({3})", p1.ToString(), p2.ToString(), ECCStandard.Point.Negate(p3).ToString(), p4.ToString()));
                                    concurrent_1.Add(string.Format("{0},{1},{2},{3}", i, j, p4.X, p4.Y));
                                    if (j == ns - 1) break;
                                    else j++;
                                }
                                if (j == ns - 1) break;
                            }
                        }

                        //Parallel.For(0, users, i =>
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
                        //            concurrent_1.Addition(string.Format("{0},{1},{2},{3}", i, j, p5.X, p5.Y));
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
                        concurrent_test.Add(ex.Message);
                    }

                }

                #endregion

                #region Pha 4 Trích xuất kết quả Máy chủ thực hiện
                ECCStandard.Point[,] AUij = new ECCStandard.Point[users, ns];

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
                                                AUij[int.Parse(values[0]), int.Parse(values[1])] = new ECCStandard.Point(BigInteger.Parse(values[2]), BigInteger.Parse(values[3]), _curve_standard);
                                            }
                                        });
                        for (int j = 0; j < ns; j++)
                        {
                            ECCStandard.Point Aj = ECCStandard.Point.InfinityPoint;
                            for (int i = 0; i < users; i++)
                            {
                                try
                                {
                                    ECCStandard.Point tmp = Aj;
                                    Aj = ECCStandard.Point.Add(AUij[i, j], tmp);
                                    concurrent_test.Add(string.Format("({0})+({1})=({2})", tmp.ToString(), AUij[i, j].ToString(), Aj.ToString()));
                                }
                                catch (Exception ex)
                                {
                                    throw;
                                }
                            }
                            concurrent_1.Add(string.Format("{0},{1},{2}", j, Aj.X, Aj.Y));
                        }
                        //Parallel.For(0, ns, (j) =>
                        //{
                        //    Aj_affine[j] = EiSiPoint.InfinityPoint;
                        //    for (int i = 0; i < users; i++)
                        //    {
                        //        EiSiPoint tmp = EiSiPoint.Addition(Aj_affine[j], AffinePoint.ToEiSiPoint(AUij[i, j]));
                        //        Aj_affine[j] = tmp;
                        //        Aj_affine[j] = Aj_affine[j] + AffinePoint.ToEiSiPoint(AUij[i, j]);
                        //    }
                        //    concurrent_1.Addition(string.Format("{0},{1},{2},{3}", j, Aj_affine[j].Nx, Aj_affine[j].Ny, Aj_affine[j].U));
                        //});
                        sw.Stop();

                        WriteFile(_sum_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                    }
                    catch (Exception ex)
                    {
                        concurrent_test.Add(ex.Message);
                    }
                    #endregion
                }
                if (_run_export_sum)
                {
                    try
                    {
                        ECCStandard.Point[] Aj = new ECCStandard.Point[ns];
                        sw.Reset();
                        sw.Start();
                        string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                        Parallel.ForEach(data_phase4, line =>
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new ECCStandard.Point(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), _curve_standard);
                        });

                        var data_loga = BRFStandard(Aj, _curve_standard, ns, max * max * users);
                        WriteFile(_get_sum_encrypt, string.Join(";", data_loga), false);
                        Sim(data_loga, muc_tin);
                        sw.Stop();
                    }
                    catch (Exception ex)
                    {
                        concurrent_test.Add(ex.Message);
                    }

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.StackTrace);
            }
            WriteFile("Log.txt", String.Join(Environment.NewLine, concurrent_test), false);
        }






        private static string _data_folder0 = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "data");

        private static string _input = "D:\\Test\\Input";
        private static string _data_folder = "D:\\Test\\OutputStandard";
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
        public static void Clear<T>(ConcurrentBag<T> concurrentBag)
        {
            while (!concurrentBag.IsEmpty)
            {
                concurrentBag.TryTake(out _);
            }
        }

        public static string[] ReadFileAsLine(string file_name)
        {
            string full_path = Path.Combine(_data_folder, file_name);
            if (File.Exists(full_path))
            {
                return File.ReadAllLines(full_path);
            }
            return new string[] { };
        }

        public static string[] ReadFileInput(string file_name)
        {
            string full_path = Path.Combine(_input, file_name);
            if (File.Exists(full_path))
            {
                return File.ReadAllLines(full_path);
            }
            return new string[] { };
        }

    }
}
