using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ECCBase16;
using EllipticES;
using EllipticModels;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Nest;
using Newtonsoft.Json;
using RSES;

namespace ServiceMayChuWeb
{
    public class RSUtilsMayChuWeb
    {
        private static readonly Curve _curve = new ECCBase16.Curve(name: ECCBase16.CurveName.secp160k1);
        private static readonly string url_server_suggest = ConfigurationManager.AppSettings["URLServerGoiY"];
        private static readonly string _url_api_receive = url_server_suggest + "/api/recommend/receive-data";
        private static long users = 0;
        private static long movies = 0;
        private static bool _is_running_pharse_1 = false;
        private static bool _is_running_pharse_3 = false;
        private static bool _is_waiting_for_any_data = false;
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



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
        private static string _data_folder = "D:\\Test\\OutputService";






        public static async Task XayDungHeGoiY()
        {

            ConcurrentBag<string> concurrent_1 = new ConcurrentBag<string>();
            ConcurrentBag<string> concurrent_2 = new ConcurrentBag<string>();

            List<UserRate> user_rates = UserRateRepository.Instance.GetAll(out _);
            List<TaiKhoan> dsach_tai_khoan = TaiKhoanRepository.Instance.GetAll(out users, 1, 99999, new string[] { "id", "index" });
            List<Phim> dsach_movie = PhimRepository.Instance.GetAll(out movies, 1, 99999, new string[] { "id", "index" });

            IEnumerable<string> dsach_id_tai_khoan = dsach_tai_khoan.Select(x => x.id);
            IEnumerable<string> dsach_id_phim = dsach_movie.Select(x => x.id);
            IEnumerable<string> dsach_id_da_rate = user_rates.Select(x => x.user_id);

            IEnumerable<string> dsach_user_chua_rate = dsach_id_tai_khoan.Except(dsach_id_da_rate);

            var group_by_user = user_rates.GroupBy(x => x.user_id);

            ///Fill lại những mục tin chưa rate
            foreach (var group in group_by_user)
            {
                IEnumerable<string> id_movie_rated = group.Select(x => x.movie_id);
                IEnumerable<string> id_movie_unrate = dsach_id_phim.Except(id_movie_rated);
                foreach (string item in id_movie_unrate)
                {
                    user_rates.Add(new UserRate
                    {
                        user_id = group.Key,
                        movie_id = item,
                        rate = 0,
                    });
                }
            }

            foreach (var user_chua_rate in dsach_user_chua_rate)
            {
                foreach (string movide_id in dsach_id_phim)
                {
                    UserRate rate = new UserRate()
                    {
                        user_id = user_chua_rate,
                        movie_id = movide_id,
                        rate = 0,
                    };
                    rate.AutoId();
                    user_rates.Add(rate);
                }
            }

            long ns = movies * (movies + 5) / 2;
            long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));

            ConcurrentTwoKeysDictionary<string, string, int> Ri = new ConcurrentTwoKeysDictionary<string, string, int>();
            ConcurrentTwoKeysDictionary<string, long, int> Rns = new ConcurrentTwoKeysDictionary<string, long, int>();

            EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);

            ConcurrentBag<string> bag_rns = new ConcurrentBag<string>();
            Parallel.ForEach(user_rates, (item) =>
            {
                Ri.Add(item.user_id, item.movie_id, item.rate);
                bag_rns.Add(string.Format("{0},{1},{2}", item.user_id, item.movie_id, item.rate));
            });
            WriteFile("rate.txt", string.Join(Environment.NewLine, bag_rns), false);
            Clear(bag_rns);

            ConcurrentBag<string> test = new ConcurrentBag<string>();
            foreach (string user_id in dsach_id_tai_khoan)
            {
                long j = 0;
                foreach (string movie_id in dsach_id_phim)
                {
                    Rns[user_id, j] = Ri[user_id, movie_id];
                    bag_rns.Add(string.Format("{0},{1},{2}", user_id, j, Rns[user_id, j]));
                    j += 1;
                }
                foreach (string movie_id in dsach_id_phim)
                {
                    int rate = Ri[user_id, movie_id] == 0 ? 0 : 1;
                    Rns[user_id, j] = rate;
                    bag_rns.Add(string.Format("{0},{1},{2}", user_id, j, Rns[user_id, j]));
                    j += 1;
                }
                foreach (string movie_id in dsach_id_phim)
                {
                    Rns[user_id, j] = Ri[user_id, movie_id] * Ri[user_id, movie_id];
                    bag_rns.Add(string.Format("{0},{1},{2}", user_id, j, Rns[user_id, j]));
                    j += 1;
                }
                long t = 3 * movies;
                for (int t2 = 0; t2 < movies - 1; t2++)
                {
                    for (int t22 = t2 + 1; t22 < movies; t22++)
                    {
                        test.Add(string.Format("{0},{1},{2},{3}", user_id, t, t2, t22));
                        Rns[user_id, t] = Ri[user_id, dsach_id_phim.ElementAt(t2)] * Ri[user_id, dsach_id_phim.ElementAt(t22)];
                        bag_rns.Add(string.Format("{0},{1},{2}", user_id, t, Rns[user_id, t]));
                        t++;
                    }
                }
            }
            WriteFile(_rns, string.Join(Environment.NewLine, bag_rns), false);
            WriteFile("test.txt", string.Join(Environment.NewLine, test), false);
            if (!_is_running_pharse_1)
            {
                Stopwatch sw = new Stopwatch();
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                try
                {
                    _is_running_pharse_1 = true;
                    sw.Start();
                    ConcurrentBag<PharseContent> list_user_key = new ConcurrentBag<PharseContent>();

                    List<PharseContent> khoa_cong_khai = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                    khoa_cong_khai.ForEach(x =>
                    {
                        concurrent_1.Add(string.Format("{0},{1},{2}", x.user_id, x.key_index, x.secret));
                        concurrent_2.Add(string.Format("{0},{1},{2},{3}", x.user_id, x.key_index, x.point.X, x.point.Y));
                    });

                    Parallel.ForEach(dsach_id_tai_khoan, (user_id) =>
                    {
                        Parallel.For(0, nk, (j) =>
                        {
                            BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                            ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                            AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                            PharseContent user_key = new PharseContent()
                            {
                                total_users = users,
                                total_movies = movies,
                                user_id = user_id,
                                key_index = j,
                                pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                                secret = secret.ToString(),
                                point = PointPharseContent.Map(pub_in_affine),
                            };
                            user_key.AutoId().SetMetaData();
                            list_user_key.Add(user_key);
                            //concurrent_1.Add(string.Format("{0},{1},{2}", user_id, j, secret));
                            //concurrent_2.Add(string.Format("{0},{1},{2}", user_id, j, pub_in_affine.ToString()));
                        });
                    });
                    sw.Stop();
                    //PharseContentRepository.Instance.IndexMany(list_user_key);
                    NoteCRM crm = new NoteCRM()
                    {
                        users = users,
                        news = movies,
                        time_complete = sw.ElapsedMilliseconds / 60000,
                        pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI
                    };
                    crm.SetMetaData();
                    NoteCRMRepository.Instance.Index(crm);
                    //IEnumerable<object> send_data = list_user_key.Select(x => new
                    IEnumerable<object> send_data = khoa_cong_khai.Select(x => new
                    {
                        x.user_id,
                        x.total_movies,
                        x.total_users,
                        x.user_index,
                        x.key_index,
                        x.point,
                        x.pharse,
                        x.id,
                    });
                    if (send_data != null && send_data.Any())
                    {
                        await _postRequest(_url_api_receive, send_data);
                    }

                    WriteFile(_key_user_prv, string.Join(Environment.NewLine, concurrent_1), false);
                    WriteFile(_key_user_pub, string.Join(Environment.NewLine, concurrent_2), false);
                    Clear(concurrent_1);
                    Clear(concurrent_2);
                }
                catch (Exception ex)
                {
                    _is_running_pharse_1 = false;
                    _logger.Error(ex);
                }
                #endregion
            }

            if (!_is_running_pharse_3)
            {
                Stopwatch sw = new Stopwatch();
                #region Pha 3 Gửi dữ liệu Những người dùng Ui thực hiện
                try
                {
                    sw.Start();
                    List<PharseContent> khoa_cong_khai = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                    List<PharseContent> khoa_dung_chung = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_DUNG_CHUNG, out _);

                    if (khoa_cong_khai.Any() && khoa_dung_chung.Any())
                    {
                        _is_running_pharse_3 = true;
                        EiSiPoint[] KPj = new EiSiPoint[nk];
                        Parallel.ForEach(khoa_dung_chung, kp =>
                        {
                            KPj[kp.key_index] = PointPharseContent.ToEiSiPoint(kp.point, _curve);
                        });
                        ConcurrentMultikeysDictionary<string, long, BigInteger> ksuij = new ConcurrentMultikeysDictionary<string, long, BigInteger>();
                        Parallel.ForEach(khoa_cong_khai, ksu =>
                        {
                            bool is_added = ksuij.TryAdd(ksu.user_id, ksu.key_index, BigInteger.Parse(ksu.secret, System.Globalization.NumberStyles.Number));
                            if (!is_added)
                            {
                                throw new Exception();
                            }
                        });

                        ConcurrentBag<PharseContent> ma_hoa_xep_hang = new ConcurrentBag<PharseContent>();

                        ConcurrentDictionary<long, EiSiPoint> dic_repeated = new ConcurrentDictionary<long, EiSiPoint>();

                        Parallel.ForEach(dsach_id_tai_khoan, (user_id) =>
                        {
                            long j = 0;
                            for (long t = 0; t < nk - 1; t++)
                            {
                                for (long k = t + 1; k < nk; k++)
                                {
                                    try
                                    {
                                        int rate = Rns[user_id, j];
                                        if (!dic_repeated.TryGetValue(rate, out EiSiPoint p1))
                                        {
                                            p1 = EiSiPoint.Multiply(rate, G);
                                            dic_repeated.TryAdd(rate, p1);
                                        }
                                        ECCBase16.EiSiPoint p2 = EiSiPoint.Base16Multiplicands(ksuij[user_id, k], KPj[t]);
                                        ECCBase16.EiSiPoint p3 = EiSiPoint.Base16Multiplicands(ksuij[user_id, t], KPj[k]);
                                        ECCBase16.EiSiPoint p4 = EiSiPoint.Subtract(EiSiPoint.Addition(p1, p2), p3);
                                        ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);
                                        concurrent_1.Add(string.Format("{0},{1},{2}", user_id, j, p5.ToString()));
                                        PharseContent pharse_content = new PharseContent
                                        {
                                            total_users = users,
                                            total_movies = movies,
                                            //user_index = i,
                                            user_id = user_id,
                                            key_index = j,
                                            point = PointPharseContent.Map(p5),
                                            pharse = Pharse.BUILD_MA_HOA_XEP_HANG,
                                        };
                                        pharse_content.AutoId().SetMetaData();
                                        ma_hoa_xep_hang.Add(pharse_content);
                                        if (j == ns - 1) break;
                                        else j++;
                                    }
                                    catch (Exception ex)
                                    {

                                        throw;
                                    }

                                }
                                if (j == ns - 1) break;
                            }
                        });
                        sw.Stop();
                        if (ma_hoa_xep_hang.Any())
                        {
                            PharseContentRepository.Instance.IndexMany(ma_hoa_xep_hang);
                        }
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
                            time_complete = sw.ElapsedMilliseconds / 60000,
                            pharse = Pharse.BUILD_MA_HOA_XEP_HANG,
                        };
                        NoteCRMRepository.Instance.Index(crm);
                        IEnumerable<object> send_data = ma_hoa_xep_hang.Select(x => new
                        {
                            x.total_movies,
                            x.total_users,
                            x.point,
                            x.pharse,
                            x.user_index,
                            x.key_index,
                            x.id,
                            x.user_id,
                        });
                        if (send_data != null && send_data.Any())
                        {
                            await _postRequest(_url_api_receive, send_data);
                        }
                        WriteFile(_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                    }
                    else
                    {
                        _is_waiting_for_any_data = true;
                    }
                }
                catch (Exception ex)
                {
                    _is_running_pharse_3 = false;
                    _logger.Error(ex);
                }
                finally
                {
                    if (!_is_waiting_for_any_data)
                    {
                        //_is_running_pharse_1 = false;
                        _is_running_pharse_3 = false;
                    }
                }
                #endregion
            }
        }
        private static async Task _postRequest(string uri, object send_data)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, uri);

                    var payload = new MultipartFormDataContent();
                    payload.Add(new StringContent(JsonConvert.SerializeObject(send_data)), "PharseContent");
                    request.Content = payload;
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                }
            }
        }

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


        private static bool _is_running_suggest = false;
        public static async Task SinhGoiY()
        {
            if (!_is_running_suggest)
            {
                try
                {
                    _is_running_suggest = true;
                    long movies = 0;
                    long last_sugg = XMedia.XUtil.TimeInEpoch(DateTime.Now.Subtract(TimeSpan.FromHours(5)));
                    List<TaiKhoan> dsach_tai_khoan = TaiKhoanRepository.Instance.GetLastSuggestion(last_sugg, 1, 99999, out _, new string[] { "username" });
                    IEnumerable<string> dsach_usernames = dsach_tai_khoan.Select(x => x.username);
                    EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);
                    Dictionary<string, EiSiPoint> user_public_keys = new Dictionary<string, EiSiPoint>();


                    List<UserRate> user_rates = UserRateRepository.Instance.GetUserRate(dsach_tai_khoan.Select(x => x.username));
                    List<Phim> dsach_movies = PhimRepository.Instance.GetAll(out movies, 1, 99999, new string[] { "id", "index" });
                    IEnumerable<string> dsach_movie_id = dsach_movies.Select(x => x.id);
                    foreach (TaiKhoan tk in dsach_tai_khoan)
                    {
                        if (!BigInteger.TryParse(tk.prv_key, out BigInteger xi))
                        {
                            xi = Numerics.RandomBetween(1, _curve.N - 1);
                        }
                        EiSiPoint pub = EiSiPoint.Base16Multiplicands(xi, G);
                        user_public_keys.Add(tk.id, pub);
                    }

                    ConcurrentTwoKeysDictionary<string, string, int> dic_users_rate = new ConcurrentTwoKeysDictionary<string, string, int>();
                    var group_by_user = user_rates.GroupBy(x => x.user_id);
                    foreach (var group in group_by_user)
                    {
                        IEnumerable<string> dsach_movie_id_rated = group.Select(x => x.movie_id);
                        foreach (var item in group)
                        {
                            dic_users_rate.Add(group.Key, item.movie_id, item.rate);
                        }
                        IEnumerable<string> dsach_movie_id_unrate = dsach_movie_id.Except(dsach_movie_id_rated);
                        foreach (var movie_id in dsach_movie_id_unrate)
                        {
                            dic_users_rate.Add(group.Key, movie_id, 0);
                        }
                    }

                    List<PharseContent> pharse_ma_hoa = new List<PharseContent>();

                    foreach (var user_key in user_public_keys)
                    {
                        if (dic_users_rate.TryGetValue(user_key.Key, out ConcurrentDictionary<string, int> user_rate))
                        {
                            AffinePoint Xi = EiSiPoint.ToAffine(user_key.Value);
                            long key_index = 0;
                            foreach (var movie_rate in user_rate)
                            {
                                BigInteger cj = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                                EiSiPoint p1 = EiSiPoint.Base16Multiplicands(movie_rate.Value, G);
                                EiSiPoint p2 = EiSiPoint.Base16Multiplicands(cj, user_key.Value);
                                EiSiPoint C1j = EiSiPoint.Addition(p1, p2);
                                EiSiPoint C2j = EiSiPoint.Base16Multiplicands(cj, G);

                                AffinePoint C1j_affine = EiSiPoint.ToAffine(C1j);
                                AffinePoint C2j_affine = EiSiPoint.ToAffine(C2j);

                                PharseContent pharse = new PharseContent()
                                {
                                    user_id = user_key.Key,
                                    point = PointPharseContent.Map(C1j_affine),
                                    point_2 = PointPharseContent.Map(C2j_affine),
                                    movie_id = movie_rate.Key,
                                    pharse = Pharse.SUGGEST_MA_HOA_VECTOR,
                                    Xi = PointPharseContent.Map(Xi),
                                    key_index=key_index,
                                };
                                pharse.AutoId().SetMetaData();
                                pharse_ma_hoa.Add(pharse);
                                key_index += 1;
                            }
                        }
                    }
                    if (pharse_ma_hoa.Any())
                    {
                        PharseContentRepository.Instance.IndexMany(pharse_ma_hoa);
                        IEnumerable<object> send_data = pharse_ma_hoa.Select(x => new
                        {
                            x.user_id,
                            x.total_movies,
                            x.total_users,
                            x.user_index,
                            x.key_index,
                            x.point,
                            x.point_2,
                            x.pharse,
                            x.id,
                            x.Xi,
                        });
                        if (send_data != null && send_data.Any())
                        {
                            await _postRequest(_url_api_receive, send_data);
                        }
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }
                
                
                _is_running_suggest = false;
            }
        }

    }
}
