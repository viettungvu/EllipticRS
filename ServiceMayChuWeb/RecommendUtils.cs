using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
using Newtonsoft.Json;
using RSES;

namespace ServiceMayChuWeb
{
    public class RecommendUtils
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
        public static async Task XayDungHeGoiY()
        {
            List<UserRate> user_rates = UserRateRepository.Instance.GetAll(out _);
            List<TaiKhoan> dsach_max_user_index = TaiKhoanRepository.Instance.GetAll(out users, 1, 99999, new string[] { "id", "index" });
            List<Phim> dsach_max_movie_index = PhimRepository.Instance.GetAll(out movies, 1, 99999, new string[] { "id", "index" });
            long total_users = user_rates.Max(x => x.user_index) + 1;
            long total_movies = user_rates.Max(x => x.movie_index) + 1;

            long ns = total_movies * (total_movies + 5) / 2;
            long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
            long[,] Ri = new long[total_users, total_movies];
            long[,] Rns = new long[total_users, ns];
            EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);

            Parallel.ForEach(user_rates, (item) =>
            {
                Ri[item.user_index - 1, item.movie_index - 1] = item.rate;
            });

            Parallel.For(0, total_users, (i) =>
            {
                Parallel.For(0, total_movies, (j) =>
                {
                    Rns[i, j] = Ri[i, j];
                });

                Parallel.For(total_movies, 2 * total_movies, (j) =>
                {
                    Rns[i, j] = Ri[i, j - total_movies] == 0 ? 0 : 1;
                });
                Parallel.For(2 * total_movies, 3 * total_movies, (j) =>
                {
                    Rns[i, j] = Ri[i, j - 2 * total_movies] * Ri[i, j - 2 * total_movies];
                });
                long t = 3 * total_movies;
                for (int t2 = 0; t2 < total_movies - 1; t2++)
                {
                    for (int t22 = t2 + 1; t22 < total_movies; t22++)
                    {
                        Rns[i, t] = Ri[i, t2] * Ri[i, t22];
                        t++;
                    }
                }
            });
            if (!_is_running_pharse_1)
            {
                Stopwatch sw = new Stopwatch();
                #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
                try
                {
                    _is_running_pharse_1 = true;
                    sw.Start();
                    ConcurrentBag<PharseContent> list_user_key = new ConcurrentBag<PharseContent>();
                    Parallel.For(0, total_users, (i) =>
                    {
                        Parallel.For(0, nk, (j) =>
                        {
                            BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                            ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                            AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                            PharseContent user_key = new PharseContent()
                            {
                                total_users = total_users,
                                total_movies = total_movies,
                                user_index = i,
                                key_index = j,
                                pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                                secret = secret.ToString(),
                                point = PointPharseContent.Map(pub_in_affine),
                            };
                            user_key.AutoId();
                            user_key.SetMetaData();
                            list_user_key.Add(user_key);
                        });
                    });
                    sw.Stop();
                    PharseContentRepository.Instance.IndexMany(list_user_key);
                    NoteCRM crm = new NoteCRM()
                    {
                        users = total_users,
                        news = total_movies,
                        time_complete = sw.ElapsedMilliseconds / 60000,
                    };
                    crm.SetMetaData();
                    NoteCRMRepository.Instance.Index(crm);

                    IEnumerable<object> send_data = list_user_key.Select(x => new
                    {
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
                }
                catch (Exception ex)
                {
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
                    List<PharseContent> data_pha_1 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                    List<PharseContent> data_pha_2 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_DUNG_CHUNG, out _);

                    if (data_pha_1.Any() && data_pha_2.Any())
                    {
                        _is_running_pharse_3 = true;
                        EiSiPoint[] KPj = new EiSiPoint[nk];
                        Parallel.ForEach(data_pha_2, kp =>
                        {
                            KPj[kp.key_index] = PointPharseContent.ToEiSiPoint(kp.point, _curve);
                        });

                        BigInteger[,] ksuij = new BigInteger[total_users, nk];
                        Parallel.ForEach(data_pha_1, ksu =>
                        {

                            ksuij[ksu.user_index, ksu.key_index] = BigInteger.Parse(ksu.secret, System.Globalization.NumberStyles.Number);
                        });

                        ConcurrentBag<PharseContent> ma_hoa_xep_hang = new ConcurrentBag<PharseContent>();

                        ConcurrentDictionary<long, EiSiPoint> dic_repeated = new ConcurrentDictionary<long, EiSiPoint>();
                        Parallel.For(0, total_users, i =>
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
                                    ECCBase16.EiSiPoint p4 = EiSiPoint.Subtract(EiSiPoint.Addition(p1, p2), p3);
                                    ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);

                                    PharseContent pharse_content = new PharseContent
                                    {
                                        total_users = total_users,
                                        total_movies = total_movies,
                                        user_index = i,
                                        key_index = j,
                                        point = PointPharseContent.Map(p5),
                                        pharse = Pharse.BUILD_MA_HOA_XEP_HANG,
                                    };
                                    pharse_content.AutoId().SetMetaData();
                                    ma_hoa_xep_hang.Add(pharse_content);
                                    if (j == ns - 1) break;
                                    else j++;
                                }
                                if (j == ns - 1) break;
                            }
                        });


                        sw.Stop();
                        PharseContentRepository.Instance.IndexMany(ma_hoa_xep_hang);
                        NoteCRM crm = new NoteCRM()
                        {
                            users = total_users,
                            news = total_movies,
                            time_complete = sw.ElapsedMilliseconds / 60000,
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
                        });
                        if (send_data != null && send_data.Any())
                        {
                            await _postRequest(_url_api_receive, send_data);
                        }
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
                        _is_running_pharse_1 = false;
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
    }
}
