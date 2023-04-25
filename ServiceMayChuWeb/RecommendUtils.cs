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
using System.Text;
using System.Threading.Tasks;
using ECCBase16;
using EllipticES;
using EllipticModels;
using Newtonsoft.Json;
using RSES;

namespace ServiceMayChuWeb
{
    public class RecommendUtils
    {
        private static readonly Curve _curve = new ECCBase16.Curve(name: ECCBase16.CurveName.secp160k1);
        private static readonly string url_server_suggest = ConfigurationManager.AppSettings["UrlWebServer"];
        public static void XayDungHeGoiY()
        {
            string url_api_receive = Path.Combine(url_server_suggest, "api\\generate-common-key");

            List<UserRate> user_rates = UserRateRepository.Instance.GetAll(out long total);
            long users = user_rates.Select(x => x.user_index).Distinct().Count();
            long news = user_rates.Select(x => x.news_index).Distinct().Count();
            int[,] Ri = new int[users, news];
            Parallel.ForEach(user_rates, item =>
            {
                Ri[item.user_index, item.news_index] = item.rate;
            });

            long ns = news * (news + 5) / 2;
            long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
            int[,] Rns = new int[users, ns];
            for (long i = 0; i < users; i++)
            {
                for (int j = 0; j < news; j++)
                {
                    Rns[i, j] = Ri[i, j];

                }
                for (long j = news; j < 2 * news; j++)
                {
                    if (Ri[i, j - news] == 0) Rns[i, j] = 0;
                    else Rns[i, j] = 1;

                }
                for (long j = 2 * news; j < 3 * news; j++)
                {
                    Rns[i, j] = Ri[i, j - 2 * news] * Ri[i, j - 2 * news];
                }

                long t = 3 * news;
                for (int t2 = 0; t2 < news - 1; t2++)
                {
                    for (int t22 = t2 + 1; t22 < news; t22++)
                    {
                        Rns[i, t] = Ri[i, t2] * Ri[i, t22];

                        t++;
                    }
                }
            }


            EiSiPoint G = AffinePoint.ToEiSiPoint(_curve.G);

            #region Pha 1 Chuẩn bị các khóa Những người dùng UI thực hiện
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
                ConcurrentBag<PharseContent> list_user_key = new ConcurrentBag<PharseContent>();
                Parallel.For(0, users, (i) =>
                {
                    Parallel.For(0, nk, (j) =>
                    {
                        BigInteger secret = ECCBase16.Numerics.RandomBetween(1, _curve.N - 1);
                        ECCBase16.EiSiPoint pub = EiSiPoint.Base16Multiplicands(secret, G);
                        AffinePoint pub_in_affine = EiSiPoint.ToAffine(pub);
                        PharseContent user_key = new PharseContent()
                        {
                            news_index = j,
                            user_index = i,
                            pharse = Pharse.BUILD_SINH_KHOA_CONG_KHAI,
                            secret = secret.ToString(),
                            point = PointPharseContent.Map(pub_in_affine),
                        };
                        user_key.AutoId();
                        list_user_key.Add(user_key);
                    });
                });
                PharseContentRepository.Instance.IndexMany(list_user_key);
                sw.Stop();
                NoteCRM crm = new NoteCRM()
                {
                    users = users,
                    news = news,
                    time_complete = sw.ElapsedMilliseconds / 60000,
                };
                NoteCRMRepository.Instance.Index(crm);

                IEnumerable<object> send_data = list_user_key.Select(x => new
                {
                    x.user_index,
                    x.news_index,
                    x.point,
                    x.pharse,
                    x.id,
                });
                if (send_data != null && send_data.Any())
                {
                    _postRequest(url_api_receive, send_data);

                }
            }
            catch (Exception ex)
            {
                throw;
            }
            #endregion

            #region Pha 3 Gửi dữ liệu Những người dùng Ui thực hiện
            try
            {
                sw.Start();
                List<PharseContent> data_pha_1 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                List<PharseContent> data_pha_2 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_DUNG_CHUNG, out _);
                users = data_pha_2.Max(x => x.user_index);
                news = data_pha_2.Max(x => x.news_index);
                ns = news * (news + 5) / 2;
                nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                EiSiPoint[] KPj = new EiSiPoint[nk];
                Parallel.ForEach(data_pha_2, kp =>
                {
                    KPj[kp.news_index] = PointPharseContent.ToEiSiPoint(kp.point, _curve);
                });

                BigInteger[,] ksuij = new BigInteger[users, news];
                Parallel.ForEach(data_pha_1, ksu =>
                {
                    ksuij[ksu.user_index, ksu.news_index] = BigInteger.Parse(ksu.secret, System.Globalization.NumberStyles.Number);
                });

                ConcurrentBag<PharseContent> ma_hoa_xep_hang = new ConcurrentBag<PharseContent>();

                ConcurrentDictionary<int, EiSiPoint> dic_repeated = new ConcurrentDictionary<int, EiSiPoint>();
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
                            ECCBase16.EiSiPoint p4 = EiSiPoint.Subtract(EiSiPoint.Addition(p1, p2), p3);
                            ECCBase16.AffinePoint p5 = EiSiPoint.ToAffine(p4);

                            PharseContent pharse_content = new PharseContent
                            {
                                user_index = i,
                                news_index = j,
                                point = PointPharseContent.Map(p5),
                                pharse = Pharse.BUILD_MA_HOA_XEP_HANG,
                            };
                            pharse_content.AutoId();
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
                    users = users,
                    news = news,
                    time_complete = sw.ElapsedMilliseconds / 60000,
                };
                NoteCRMRepository.Instance.Index(crm);
                IEnumerable<object> send_data = ma_hoa_xep_hang.Select(x => new
                {
                    x.user_index,
                    x.news_index,
                    x.point,
                    x.pharse,
                    x.id,
                });
                if (send_data != null && send_data.Any())
                {
                    _postRequest(url_api_receive, send_data);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            #endregion
        }


        private static async void _postRequest(string uri, object send_data)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                using (HttpClient client = new HttpClient())
                {
                    MultipartFormDataContent form_data = new MultipartFormDataContent();
                    form_data.Add(new StringContent(JsonConvert.SerializeObject(send_data)), "PharseContent");
                    form_data.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    await client.PostAsync(uri, form_data);
                }
            }
        }

        public static void GenData()
        {
            long users = 10;
            long news = 20;
            List<UserRate> user_rates = new List<UserRate>();
            Random rd = new Random();

            for (int i = 0; i < users; i++)
            {
                for (int j = 0; j < news; j++)
                {
                    UserRate ur = new UserRate
                    {
                        news_index = j,
                        user_index = i,
                        rate = rd.Next(0, 5),
                    };
                    ur.AutoId();
                    user_rates.Add(ur);
                }
            }
            UserRateRepository.Instance.IndexMany(user_rates);
        }

    }
}
