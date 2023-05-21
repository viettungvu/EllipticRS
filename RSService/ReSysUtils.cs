using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using RSES;

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

        private static readonly string _cf_cipher_user_part_1 = "1.2.RunCF.CipherTextUserPart1.txt";
        private static readonly string _cf_cipher_user_part_2 = "1.2.RunCF.CipherTextUserPart2.txt";
        #endregion

        private static bool _run_phase_1 = true;
        private static bool _run_phase_2 = true;
        private static bool _run_phase_3 = true;
        private static bool _run_phase_4 = true;
        private static bool _run_export_sum = true;

        private static int max = 5;
        //private static int users = 943;
        //private static int movies = 200;


        private static int users = 5;
        private static int movies = 200;
        private static int ns = movies * (movies + 5) / 2;
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
                for (int j = 0; j < muc_tin; j++)
                {
                    for (int k = j; k < muc_tin; k++)
                    {
                        if (j == k)
                        {
                            sim[j, k] = 1;
                        }
                        else
                        {
                            sim[j, k] = sum[3 * muc_tin + l] / (Math.Sqrt(sum[2 * muc_tin + j]) * Math.Sqrt(sum[2 * muc_tin + k]));
                            l++;
                        }
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


                int[,] Ri = new int[users, movies];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < movies; j++)
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
                    //UserRate rate = new UserRate()
                    //{
                    //    user_id = (int.Parse(values[0]) - 1).ToString(),
                    //    movie_id = (int.Parse(values[1]) - 1).ToString(),
                    //    rate = int.Parse(values[2]),
                    //};
                    //rate.AutoId().SetMetaData();
                    //rates.Add(rate);
                });

                //UserRateRepository.Instance.IndexMany(rates);
                ConcurrentBag<string> bag_rns = new ConcurrentBag<string>();

                int[,] Rns = new int[users, ns];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < movies; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                        bag_rns.Add(string.Format("{0},{1},{2}", i, j, Rns[i, j]));
                    }
                    for (int j = movies; j < 2 * movies; j++)
                    {
                        if (Ri[i, j - movies] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                        bag_rns.Add(string.Format("{0},{1},{2}", i, j, Rns[i, j]));
                    }
                    for (int j = 2 * movies; j < 3 * movies; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * movies] * Ri[i, j - 2 * movies];
                        bag_rns.Add(string.Format("{0},{1},{2}", i, j, Rns[i, j]));
                    }

                    int t = 3 * movies;
                    for (int t2 = 0; t2 < movies - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < movies; t22++)
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
                    try
                    {
                        sw.Start();
                        ConcurrentBag<PharseContent> list = new ConcurrentBag<PharseContent>();

                        for (int i = 0; i < users; i++)
                        {
                            for (int j = 0; j < nk; j++)
                            {
                                try
                                {
                                    BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                                    ksuij[i, j] = secret;
                                    AffinePoint pub_in_affine = EiSiPoint.MulBase16Aff(secret, G);
                                    concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                                    concurrent_2.Add(string.Format("{0},{1},{2}", i, j, pub_in_affine.ToString()));

                                    PharseContent content = new PharseContent()
                                    {
                                        user_id = i.ToString(),
                                        key_index = j,
                                        secret = secret.ToString(),
                                        point = PointPharseContent.Map(pub_in_affine),
                                        pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                                        total_movies = movies,
                                        total_users = users,
                                    };
                                    content.AutoId().SetMetaData();
                                    list.Add(content);
                                }
                                catch (Exception ex)
                                {


                                }
                            }
                        }
                        //Parallel.For(0, users, (i) =>
                        //{
                        //    Parallel.For(0, nk, (j) =>
                        //    {
                        //        try
                        //        {
                        //            BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                        //            ksuij[i, j] = secret;
                        //            AffinePoint pub_in_affine = EiSiPoint.MulBase16Aff(secret, G);
                        //            concurrent_1.Add(string.Format("{0},{1},{2}", i, j, secret));
                        //            concurrent_2.Add(string.Format("{0},{1},{2}", i, j, pub_in_affine.ToString()));

                        //            PharseContent content = new PharseContent()
                        //            {
                        //                user_id = i.ToString(),
                        //                key_index = j,
                        //                secret = secret.ToString(),
                        //                point = PointPharseContent.Map(pub_in_affine),
                        //                pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                        //                total_movies = movies,
                        //                total_users = users,
                        //            };
                        //            content.AutoId().SetMetaData();
                        //            list.Add(content);
                        //        }
                        //        catch (Exception ex)
                        //        {


                        //        }

                        //    });
                        //});
                        sw.Stop();
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
                            time_complete = sw.ElapsedMilliseconds,
                            pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                        };
                        crm.SetProp();
                        NoteCRMRepository.Instance.Index(crm);
                        //PharseContentRepository.Instance.IndexMany(list);

                    }
                    catch (Exception ex)
                    {
                        throw;
                    }

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
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
                            time_complete = sw.ElapsedMilliseconds,
                            pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                        };
                        crm.SetProp();
                        NoteCRMRepository.Instance.Index(crm);
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
                        Parallel.For(0, users, i =>
                        {
                            int j = 0;
                            for (int t = 0; t < nk - 1; t++)
                            {
                                for (int k = t + 1; k < nk; k++)
                                {
                                    EiSiPoint p1 = EiSiPoint.Multiply(Rns[i, j], G);
                                    ECCBase16.EiSiPoint p2 = EiSiPoint.Base16Multiplicands(ksuij[i, k], KPj[t]);
                                    ECCBase16.EiSiPoint p3 = EiSiPoint.Base16Multiplicands(ksuij[i, t], KPj[k]);

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
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
                            time_complete = sw.ElapsedMilliseconds,
                            pharse = Pharse.BUILD_MA_HOA_XEP_HANG,
                        };
                        crm.SetProp();
                        NoteCRMRepository.Instance.Index(crm);

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
                        sw.Reset();
                        sw.Start();
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
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
                            time_complete = sw.ElapsedMilliseconds,
                            pharse = Pharse.BUILD_TINH_TONG_BAO_MAT,
                        };
                        crm.SetProp();
                        NoteCRMRepository.Instance.Index(crm);
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
                        string[] data_phase4 = ReadFileAsLine(_sum_encrypt);
                        Parallel.ForEach(data_phase4, line =>
                        {
                            string[] values = line.Split(',');
                            Aj[int.Parse(values[0])] = new AffinePoint(BigInteger.Parse(values[1]), BigInteger.Parse(values[2]), _curve);
                        });
                        sw.Reset();
                        sw.Start();
                        int[] data_loga = BRFStandard(Aj, ns, max * max * users);
                        Sim(data_loga, movies);
                        sw.Stop();
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
                            time_complete = sw.ElapsedMilliseconds,
                            pharse = Pharse.CALCULATE_SIMILAR,
                        };
                        crm.SetProp();
                        NoteCRMRepository.Instance.Index(crm);
                        WriteFile(_get_sum_encrypt, string.Join(";", data_loga), false);
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
            try
            {

                EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);
                ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
                ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();
                ConcurrentBag<string> concurrent_3 = new ConcurrentBag<string>();
                ConcurrentBag<string> concurrent_4 = new ConcurrentBag<string>();


                #region Pha 1:User target tạo khóa bí mật và khóa công khai 

                Stopwatch sw = new Stopwatch();
                sw.Start();
                BigInteger xi = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                AffinePoint Xi = EiSiPoint.ToAffine(EiSiPoint.Base16Multiplicands(xi, G));
                sw.Stop();

                #endregion


                #region Pha 2: User target mã hóa xếp hạng
                string[] str_rate_avg = ReadFileAsLine(_rate_avg);
                int[] rate_round_avg = new int[movies];

                Parallel.ForEach(str_rate_avg, line =>
                {
                    string[] values = line.Split(',');
                    double.TryParse(values[1], out double rate_avg);
                    int avg_rounded = (int)(rate_avg * 10);
                    rate_round_avg[int.Parse(values[0])] = avg_rounded;
                });


                string[] data = ReadFileInput("Data.txt");
                int[,] Ri = new int[users, movies];
                Parallel.ForEach(data, line =>
                {
                    string[] values = line.Split(',');
                    Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);
                });
                sw.Start();
                Parallel.For(0, movies, (j) =>
                {
                    BigInteger cj = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                    EiSiPoint p1 = EiSiPoint.Base16Multiplicands(Ri[0, j] * 10, G);
                    EiSiPoint p2 = EiSiPoint.Base16Multiplicands(cj, AffinePoint.ToEiSiPoint(Xi));
                    EiSiPoint C1j = EiSiPoint.Addition(p1, p2);

                    AffinePoint C1j_affine = EiSiPoint.ToAffine(C1j);
                    AffinePoint C2j_affine = EiSiPoint.MulBase16Aff(cj, G);

                    concurrent_1.Add(string.Format("{0},{1}", j, C1j_affine.ToString()));
                    concurrent_2.Add(string.Format("{0},{1}", j, C2j_affine.ToString()));
                });
                sw.Stop();
                NoteCRM crm = new NoteCRM()
                {
                    users = users,
                    news = movies,
                    time_complete = sw.ElapsedMilliseconds,
                    pharse = Pharse.SUGGEST_MA_HOA_VECTOR,
                };
                crm.SetProp();
                NoteCRMRepository.Instance.Index(crm);

                WriteFile(_cf_cipher_user_part_1, string.Join(Environment.NewLine, concurrent_1), false);
                WriteFile(_cf_cipher_user_part_2, string.Join(Environment.NewLine, concurrent_2), false);
                Clear(concurrent_1);
                Clear(concurrent_2);
                #endregion

                #region Pha 2:
                string[] str_sim_round = ReadFileAsLine(_sim_round);
                int[,] sim_rounded = new int[movies, movies];
                Parallel.ForEach(str_sim_round, line =>
                {
                    string[] values = line.Split(',');
                    sim_rounded[int.Parse(values[0]), int.Parse(values[1])] = int.Parse(values[2]);
                });



                EiSiPoint[] ctext_part_1 = new EiSiPoint[movies];
                EiSiPoint[] ctext_part_2 = new EiSiPoint[movies];

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
                sw.Reset();
                sw.Start();
                BigInteger c1 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);

                EiSiPoint[] f9 = new EiSiPoint[movies];
                EiSiPoint[] f10 = new EiSiPoint[movies];
                EiSiPoint[] f11 = new EiSiPoint[movies];

                Parallel.For(0, movies, (k) =>
                {
                    EiSiPoint sum5 = EiSiPoint.InfinityPoint;
                    BigInteger c2 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                    BigInteger c3 = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                    for (int j = k; j < movies; j++)
                    {
                        EiSiPoint skj_g = EiSiPoint.Base16Multiplicands(sim_rounded[k, j], G);
                        sum5 = EiSiPoint.Addition(sum5, skj_g);
                    }
                    EiSiPoint f5k = EiSiPoint.Addition(EiSiPoint.Multiply(rate_round_avg[k], sum5), EiSiPoint.Base16Multiplicands(c2, AffinePoint.ToEiSiPoint(Xi)));
                    EiSiPoint f6k = EiSiPoint.Base16Multiplicands(c2, G);
                    EiSiPoint f7k = EiSiPoint.Addition(EiSiPoint.Multiply(rate_round_avg[k], G), EiSiPoint.Base16Multiplicands(c3, AffinePoint.ToEiSiPoint(Xi)));
                    EiSiPoint f8k = EiSiPoint.Base16Multiplicands(c3, G);


                    EiSiPoint sum_f9k = EiSiPoint.InfinityPoint;
                    for (int j = k; j < movies; j++)
                    {
                        EiSiPoint p = EiSiPoint.Multiply(sim_rounded[k, j], EiSiPoint.Subtract(ctext_part_1[j], f7k));
                        sum_f9k = EiSiPoint.Addition(sum_f9k, p);
                    }

                    f9[k] = EiSiPoint.Addition(f5k, sum_f9k);

                    EiSiPoint sum_f10k = EiSiPoint.InfinityPoint;
                    for (int j = k; j < movies; j++)
                    {
                        EiSiPoint p = EiSiPoint.Multiply(sim_rounded[k, j], EiSiPoint.Subtract(ctext_part_2[j], f8k));
                        sum_f10k = EiSiPoint.Addition(sum_f10k, p);
                    }
                    f10[k] = EiSiPoint.Addition(f6k, sum_f10k);

                    EiSiPoint sum_f11 = EiSiPoint.InfinityPoint;
                    for (int j = k; j < movies; j++)
                    {
                        EiSiPoint p = EiSiPoint.Multiply(sim_rounded[k, j], G);
                        sum_f11 = EiSiPoint.Addition(sum_f11, p);
                    }

                    f11[k] = EiSiPoint.Addition(EiSiPoint.Base16Multiplicands(c1, AffinePoint.ToEiSiPoint(Xi)), sum_f11);
                });

                EiSiPoint f12 = EiSiPoint.Base16Multiplicands(c1, G);
                sw.Stop();
                crm = new NoteCRM()
                {
                    users = users,
                    news = movies,
                    time_complete = sw.ElapsedMilliseconds,
                    pharse = Pharse.SUGGEST_MA_HOA_DO_TUONG_TU,
                };
                crm.SetProp();
                NoteCRMRepository.Instance.Index(crm);
                //test
                //AffinePoint[] testC6 = new AffinePoint[movies];
                //for (int k = 0; k < movies; k++)
                //{
                //    int sum = 0;
                //    for (int j = k; j < movies; j++)
                //    {
                //        sum += sim_rounded[k, j];
                //    }
                //    EiSiPoint tmp = EiSiPoint.Base16Multiplicands(sum, G);
                //    testC6[k] = EiSiPoint.ToAffine(tmp);
                //}

                //AffinePoint[] testck6 = new AffinePoint[movies];
                //for (int k = 0; k < movies; k++)
                //{
                //    int sum1 = 0;
                //    int sum2 = 0;
                //    for (int j = k; j < movies; j++)
                //    {
                //        sum1 += sim_rounded[k, j];
                //        sum2 += (Ri[0, j] * 10 - rate_round_avg[j]) * sim_rounded[k, j];
                //    }
                //    EiSiPoint tmp = EiSiPoint.Base16Multiplicands(sum1 * rate_round_avg[k] + sum2, G);
                //    testck6[k] = EiSiPoint.ToAffine(tmp);
                //}




                ///Pharse 3
                ///
                sw.Reset();
                sw.Start();
               
                Parallel.For(0, movies, (k) =>
                {
                    EiSiPoint Ck = EiSiPoint.Subtract(f9[k], EiSiPoint.Base16Multiplicands(xi, f10[k]));
                    EiSiPoint C6 = EiSiPoint.Subtract(f11[k], EiSiPoint.Base16Multiplicands(xi, f12));


                    AffinePoint tmp_c6 = EiSiPoint.ToAffine(C6);
                    AffinePoint tmp_ck = EiSiPoint.ToAffine(Ck);
                    long d = Logarit(tmp_c6);
                    long dk = Logarit(tmp_ck);
                    concurrent_1.Add(String.Format("{0},{1},{2},{3},{4}", 0, k, dk, d, (double)dk / d));
                });
                sw.Stop();
                crm = new NoteCRM()
                {
                    users = users,
                    news = movies,
                    time_complete = sw.ElapsedMilliseconds,
                    pharse = Pharse.SUGGEST_DU_DOAN_XEP_HANG,
                };
                crm.SetProp();
                NoteCRMRepository.Instance.Index(crm);

                WriteFile("1.3.pik.txt", string.Join(Environment.NewLine, concurrent_1), false);
                #endregion
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        public static void RunCFB()
        {
            EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);
            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_3 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_4 = new ConcurrentBag<string>();


            #region Pha 1:User target tạo khóa bí mật và khóa công khai 
            BigInteger xi = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
            AffinePoint Xi = EiSiPoint.ToAffine(EiSiPoint.Base16Multiplicands(xi, G));


            #endregion


            #region Pha 2: User target mã hóa xếp hạng
            string[] str_rate_avg = ReadFileAsLine(_rate_avg);

            string[] data = ReadFileInput("Data.txt");
            int[,] Ri = new int[users, movies];
            Parallel.ForEach(data, line =>
            {
                string[] values = line.Split(',');
                Ri[int.Parse(values[0]) - 1, int.Parse(values[1]) - 1] = int.Parse(values[2]);

            });
            for (int j = 0; j < movies; j++)
            {
                BigInteger cj = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                EiSiPoint p1 = EiSiPoint.Base16Multiplicands(Ri[0, j] * 10, G);
                EiSiPoint p2 = EiSiPoint.Base16Multiplicands(cj, AffinePoint.ToEiSiPoint(Xi));
                EiSiPoint C1j = EiSiPoint.Addition(p1, p2);
                EiSiPoint C2j = EiSiPoint.Base16Multiplicands(cj, G);

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
            int[,] sim_rounded = new int[movies, movies];
            Parallel.ForEach(str_sim_round, line =>
            {
                string[] values = line.Split(',');
                int i = int.Parse(values[0]);
                int j = int.Parse(values[1]);
                if (i == j)
                {
                    sim_rounded[i, j] = 100;
                }
                else
                {
                    sim_rounded[i, j] = int.Parse(values[2]);
                }
            });



            EiSiPoint[] ctext_part_1 = new EiSiPoint[movies];
            EiSiPoint[] ctext_part_2 = new EiSiPoint[movies];

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


            EiSiPoint[] f1 = new EiSiPoint[movies];
            EiSiPoint[] f2 = new EiSiPoint[movies];
            EiSiPoint f3 = EiSiPoint.InfinityPoint;
            BigInteger c = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
            for (long k = 0; k < movies; k++)
            {
                EiSiPoint sumf1 = EiSiPoint.InfinityPoint;
                for (long j = k; j < movies; j++)
                {
                    f1[k] = EiSiPoint.Addition(f1[k], EiSiPoint.Base16Multiplicands(sim_rounded[k, j], ctext_part_1[j]));
                    f2[k] = EiSiPoint.Addition(f2[k], EiSiPoint.Base16Multiplicands(sim_rounded[k, j], ctext_part_2[j]));
                    EiSiPoint tmp = f3;
                    f3 = EiSiPoint.Addition(tmp, EiSiPoint.Base16Multiplicands(sim_rounded[k, j], G));
                }
            }
            AffinePoint testAff = EiSiPoint.ToAffine(f3);

            EiSiPoint f3_tmp = f3;
            f3 = EiSiPoint.Addition(f3_tmp, EiSiPoint.Base16Multiplicands(c, AffinePoint.ToEiSiPoint(Xi)));
            EiSiPoint f4 = EiSiPoint.Base16Multiplicands(c, G);

            EiSiPoint C = EiSiPoint.Subtract(f3, EiSiPoint.Base16Multiplicands(xi, f4));
            AffinePoint tmp_c6 = EiSiPoint.ToAffine(C);
            long d = Logarit(tmp_c6);



            #region Test
            int sum = 0;
            for (int k = 0; k < movies; k++)
            {
                for (int j = k; j < movies; j++)
                {
                    sum += sim_rounded[k, j];
                }
            }
            EiSiPoint test = EiSiPoint.Base16Multiplicands(sum, G);
            testAff = EiSiPoint.ToAffine(test);

            #endregion
            for (int k = 0; k < movies; k++)
            {
                EiSiPoint Ck = EiSiPoint.Subtract(f1[k], EiSiPoint.Base16Multiplicands(xi, f2[k]));
                AffinePoint tmp_ck = EiSiPoint.ToAffine(Ck);
                long dk = Logarit(tmp_ck);
                concurrent_1.Add(String.Format("{0},{1},{2},{3},{4}", 0, k, dk, d, (double)dk / d));
            }
            WriteFile("1.3.pik.txt", string.Join(Environment.NewLine, concurrent_1), false);

        }


        private static ConcurrentDictionary<long, AffinePoint> _cache_affine = new ConcurrentDictionary<long, AffinePoint>();

        public static long Logarit(AffinePoint p)
        {
            try
            {
                var exist = _cache_affine.FirstOrDefault(x => x.Value.X == p.Y && x.Value.Y == p.Y);
                if (exist.Key != 0 && exist.Value != null)
                {
                    return exist.Key;
                }
                AffinePoint sum = AffinePoint.InfinityPoint;
                long max = 10000 * movies;
                for (long i = 0; i <= max; i++)
                {
                    if (_cache_affine.TryGetValue(i, out AffinePoint s))
                    {
                        sum = s;
                    }
                    else
                    {
                        sum = AffinePoint.Addition(sum, _curve.G);
                        _cache_affine.TryAdd(i, sum);
                    }
                    if (sum.X == p.X && sum.Y == p.Y)
                    {
                        return i;
                    }

                    //if (_cache_affine.TryGetValue(i, out AffinePoint val))
                    //{
                    //    if (val.X == p.X && val.Y == p.Y)
                    //    {
                    //        return i;
                    //    }
                    //}
                    //else
                    //{
                    //    if (sum.X == p.X && sum.Y == p.Y)
                    //    {
                    //        return i;
                    //    }
                    //    else
                    //    {
                    //        sum = AffinePoint.Addition(sum, _curve.G);
                    //        _cache_affine.TryAdd(i, sum);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return 0;
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

                int ns = movies * (movies + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[users, movies];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < movies; j++)
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
                    for (int j = 0; j < movies; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                    }
                    for (int j = movies; j < 2 * movies; j++)
                    {
                        if (Ri[i, j - movies] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                    }
                    for (int j = 2 * movies; j < 3 * movies; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * movies] * Ri[i, j - 2 * movies];
                    }

                    int t = 3 * movies;
                    for (int t2 = 0; t2 < users - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < movies; t22++)
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
                        Sim(data_loga, movies);
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

                int ns = movies * (movies + 5) / 2;
                int nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                int[,] Ri = new int[users, movies];
                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < movies; j++)
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
                    for (int j = 0; j < movies; j++)
                    {
                        Rns[i, j] = Ri[i, j];
                    }
                    for (int j = movies; j < 2 * movies; j++)
                    {
                        if (Ri[i, j - movies] == 0) Rns[i, j] = 0;
                        else Rns[i, j] = 1;
                    }
                    for (int j = 2 * movies; j < 3 * movies; j++)
                    {
                        Rns[i, j] = Ri[i, j - 2 * movies] * Ri[i, j - 2 * movies];
                    }

                    int t = 3 * movies;
                    for (int t2 = 0; t2 < movies - 1; t2++)
                    {
                        for (int t22 = t2 + 1; t22 < movies; t22++)
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
                        Sim(data_loga, movies);
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
        private static string _data_folder = "D:\\Test\\OutputEiSiFull";
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
#endregion