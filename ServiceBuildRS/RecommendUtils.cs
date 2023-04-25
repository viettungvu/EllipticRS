using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ECCBase16;
using EllipticES;
using EllipticModels;
using Newtonsoft.Json;
using RSES;

namespace ServiceBuildRS
{
    public class RecommendUtils
    {
        private static Curve _curve = new ECCBase16.Curve(ECCBase16.CurveName.secp160k1);
        private const int _max = 5;
        public static void XayDungHeGoiY()
        {

            Stopwatch sw = new Stopwatch();
            long users;
            long news;
            #region Pha 2 Tính các khóa công khai dùng chung Máy chủ thực hiện
            try
            {
                List<PharseContent> data_pha_1 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                users = data_pha_1.Max(x => x.user_index);
                news = data_pha_1.Max(x => x.news_index);
                long ns = news * (news + 5) / 2;
                long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                EiSiPoint[,] KPUij = new EiSiPoint[users, news];
                Parallel.ForEach(data_pha_1, key =>
                {
                    KPUij[key.user_index, key.news_index] = PointPharseContent.ToEiSiPoint(key.point, _curve);
                });

                ConcurrentBag<PharseContent> list_pharse_2 = new ConcurrentBag<PharseContent>();
                sw.Start();
                Parallel.For(0, nk, j =>
                {
                    EiSiPoint KPj = EiSiPoint.InfinityPoint;
                    for (int i = 0; i < users; i++)
                    {
                        EiSiPoint tmp = KPj;
                        KPj = EiSiPoint.Addition(tmp, KPUij[i, j]);
                    }
                    ECCBase16.AffinePoint sum = EiSiPoint.ToAffine(KPj);

                    PharseContent user_key = new PharseContent()
                    {
                        news_index = j,
                        pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                        point = PointPharseContent.Map(sum),
                    };
                    user_key.AutoId();
                    list_pharse_2.Add(user_key);
                });
                PharseContentRepository.Instance.IndexMany(list_pharse_2);
                sw.Stop();
                NoteCRM crm = new NoteCRM()
                {
                    users = users,
                    news = news,
                    time_complete = sw.ElapsedMilliseconds / 1000,
                    pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                };
                NoteCRMRepository.Instance.Index(crm);

                IEnumerable<object> send_data = list_pharse_2.Select(x => new
                {
                    x.news_index,
                    x.point,
                    x.pharse,
                    x.id,
                });
                PostRequest("", send_data);

            }
            catch (Exception ex)
            {
                throw;
            }

            #endregion

            #region Pha 4 Trích xuất kết quả Máy chủ thực hiện
            try
            {
                sw.Reset();
                sw.Start();
                List<PharseContent> data_pha_3 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                users = data_pha_3.Max(x => x.user_index);
                news = data_pha_3.Max(x => x.news_index);
                long ns = news * (news + 5) / 2;
                long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                ECCBase16.AffinePoint[,] AUij = new ECCBase16.AffinePoint[users, ns];
                Parallel.ForEach(data_pha_3, aui =>
                {
                    AUij[aui.user_index, aui.news_index] = PointPharseContent.ToAffinePoint(aui.point, _curve);
                });
                ConcurrentBag<PharseContent> list_pharse_4 = new ConcurrentBag<PharseContent>();
                Parallel.For(0, ns, (j) =>
                {
                    EiSiPoint sum = EiSiPoint.InfinityPoint;
                    for (int i = 0; i < users; i++)
                    {
                        EiSiPoint tmp = sum;
                        sum = EiSiPoint.Addition(tmp, AffinePoint.ToEiSiPoint(AUij[i, j]));
                    }
                    AffinePoint affine = EiSiPoint.ToAffine(sum);
                    PharseContent pharse_4 = new PharseContent
                    {
                        news_index = j,
                        point = PointPharseContent.Map(affine),
                        pharse = Pharse.BUILD_TINH_TONG_BAO_MAT,
                    };
                    pharse_4.AutoId();
                    list_pharse_4.Add(pharse_4);
                });
                sw.Stop();
                PharseContentRepository.Instance.IndexMany(list_pharse_4);
                NoteCRM crm = new NoteCRM()
                {
                    users = users,
                    news = news,
                    time_complete = sw.ElapsedMilliseconds / 1000,
                    pharse = Pharse.BUILD_TINH_TONG_BAO_MAT,
                };
                NoteCRMRepository.Instance.Index(crm);


                try
                {
                    AffinePoint[] Aj = new AffinePoint[ns];
                    sw.Reset();
                    sw.Start();
                    Parallel.ForEach(list_pharse_4, aj =>
                    {
                        Aj[aj.news_index] = PointPharseContent.ToAffinePoint(aj.point, _curve);
                    });
                    int[] data_loga = _brfStandard(Aj, ns, _max * _max * users);
                    _calculateSimilary(data_loga, news);
                    sw.Stop();
                    NoteCRM cal_similar = new NoteCRM()
                    {
                        users = users,
                        news = news,
                        time_complete = sw.ElapsedMilliseconds / 1000,
                        pharse = Pharse.CALCULATE_SIMILAR,
                    };
                    NoteCRMRepository.Instance.Index(cal_similar);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            #endregion
        }

        private static int[] _brfStandard(ECCBase16.AffinePoint[] Aj, long ns, long max)
        {
            int[] result = new int[ns];
            AffinePoint K_sum = AffinePoint.InfinityPoint;
            for (int i = 0; i <= max; i++)
            {
                AffinePoint tmp = K_sum;
                K_sum = AffinePoint.Addition(tmp, _curve.G);
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
        private static void _calculateSimilary(int[] sum, long muc_tin)
        {
            double[] R = new double[muc_tin];
            double[,] sim = new double[muc_tin, muc_tin];
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            Parallel.For(0, muc_tin, j =>
            {
                if (sum[j + muc_tin] == 0)
                {
                    R[j] = 0;
                }
                else
                {
                    R[j] = (double)sum[j] / sum[j + muc_tin];
                }
            });

            int l = 0;
            try
            {
                for (int j = 0; j < muc_tin - 1; j++)
                {
                    for (int k = j + 1; k < muc_tin; k++)
                    {
                        sim[j, k] = sum[3 * muc_tin + l] / (Math.Sqrt(sum[2 * muc_tin + j]) * Math.Sqrt(sum[2 * muc_tin + k]));
                        l++;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public static async void PostRequest(string uri, object send_data)
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

    }
}
