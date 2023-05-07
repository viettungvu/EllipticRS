using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string _url_web_server = XMedia.XUtil.ConfigurationManager.AppSetting["UrlWebServer"];
        private static readonly string _url_api_receive = _url_web_server + "/api/recommend/receive-data";

        private static bool _is_running_pharse_2 = false;
        private static bool _is_running_pharse_4 = false;
        private static bool _is_waiting_for_any_data = false;
        public static async Task XayDungHeGoiY()
        {
            if (!_is_running_pharse_2)
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    #region Pha 2 Tính các khóa công khai dùng chung Máy chủ thực hiện
                    try
                    {
                        _is_running_pharse_2 = true;
                        List<PharseContent> data_pha_1 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                        if (data_pha_1.Any())
                        {
                            long total_users = data_pha_1.Max(x => x.total_users);
                            long total_movies = data_pha_1.Max(x => x.total_movies);
                            long ns = total_movies * (total_movies + 5) / 2;
                            long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));


                            EiSiPoint[,] KPUij = new EiSiPoint[total_users, nk];
                            Parallel.ForEach(data_pha_1, item =>
                            {
                                KPUij[item.user_index, item.key_index] = PointPharseContent.ToEiSiPoint(item.point, _curve);
                            });

                            ConcurrentBag<PharseContent> list_pharse_2 = new ConcurrentBag<PharseContent>();
                            sw.Start();
                            Parallel.For(0, nk, j =>
                            {
                                EiSiPoint KPj = EiSiPoint.InfinityPoint;
                                for (int i = 0; i < total_users; i++)
                                {
                                    EiSiPoint tmp = KPj;
                                    KPj = EiSiPoint.Addition(tmp, KPUij[i, j]);
                                }
                                ECCBase16.AffinePoint sum = EiSiPoint.ToAffine(KPj);

                                PharseContent user_key = new PharseContent()
                                {
                                    total_movies = total_movies,
                                    total_users = total_users,
                                    key_index = j,
                                    pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                                    point = PointPharseContent.Map(sum),
                                };
                                user_key.AutoId();
                                user_key.SetMetaData();
                                list_pharse_2.Add(user_key);
                            });
                            sw.Stop();
                            PharseContentRepository.Instance.IndexMany(list_pharse_2);
                            NoteCRM crm = new NoteCRM()
                            {
                                users = total_users,
                                news = total_movies,
                                time_complete = sw.ElapsedMilliseconds / 1000,
                                pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                            };
                            crm.SetMetaData();
                            NoteCRMRepository.Instance.Index(crm);

                            IEnumerable<object> send_data = list_pharse_2.Select(x => new
                            {
                                x.key_index,
                                x.point,
                                x.pharse,
                                x.id,
                                x.total_movies,
                                x.total_users,
                            });
                            if (send_data != null && send_data.Any())
                            {
                                await _sendRequest(_url_api_receive, send_data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
            if (!_is_running_pharse_4)
            {
                Stopwatch sw = new Stopwatch();
                #region Pha 4 Trích xuất kết quả Máy chủ thực hiện
                try
                {
                    sw.Start();
                    List<PharseContent> data_pha_3 = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_MA_HOA_XEP_HANG, out _);
                    if (data_pha_3.Any())
                    {
                        _is_running_pharse_4 = true;
                        long total_users = data_pha_3.Max(x => x.total_users);
                        long total_movies = data_pha_3.Max(x => x.total_movies);
                        long ns = total_movies * (total_movies + 5) / 2;
                        long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));
                        ECCBase16.AffinePoint[,] AUij = new ECCBase16.AffinePoint[total_users, ns];
                        Parallel.ForEach(data_pha_3, aui =>
                        {
                            AUij[aui.user_index, aui.key_index] = PointPharseContent.ToAffinePoint(aui.point, _curve);
                        });
                        ConcurrentBag<PharseContent> list_pharse_4 = new ConcurrentBag<PharseContent>();
                        Parallel.For(0, ns, (j) =>
                        {
                            EiSiPoint sum = EiSiPoint.InfinityPoint;
                            for (int i = 0; i < total_users; i++)
                            {
                                EiSiPoint tmp = sum;
                                sum = EiSiPoint.Addition(tmp, AffinePoint.ToEiSiPoint(AUij[i, j]));
                            }
                            AffinePoint affine = EiSiPoint.ToAffine(sum);
                            PharseContent pharse_4 = new PharseContent
                            {
                                key_index = j,
                                point = PointPharseContent.Map(affine),
                                pharse = Pharse.BUILD_TINH_TONG_BAO_MAT,
                            };
                            pharse_4.AutoId().SetMetaData();
                            list_pharse_4.Add(pharse_4);
                        });
                        sw.Stop();
                        PharseContentRepository.Instance.IndexMany(list_pharse_4);
                        NoteCRM crm = new NoteCRM()
                        {
                            users = total_users,
                            news = total_movies,
                            time_complete = sw.ElapsedMilliseconds / 1000,
                            pharse = Pharse.BUILD_TINH_TONG_BAO_MAT,
                        };
                        crm.SetMetaData();
                        NoteCRMRepository.Instance.Index(crm);
                        try
                        {
                            sw.Reset();
                            sw.Start();
                            AffinePoint[] Aj = new AffinePoint[ns];
                            Parallel.ForEach(list_pharse_4, aj =>
                            {
                                Aj[aj.key_index] = PointPharseContent.ToAffinePoint(aj.point, _curve);
                            });
                            int[] data_loga = _brfStandard(Aj, ns, _max * _max * total_users);
                            List<PharseContent> similary = _calculateSimilary(data_loga, total_movies);
                            PharseContentRepository.Instance.IndexMany(similary);
                            sw.Stop();
                            NoteCRM cal_similar = new NoteCRM()
                            {
                                users = total_users,
                                news = total_movies,
                                time_complete = sw.ElapsedMilliseconds / 1000,
                                pharse = Pharse.CALCULATE_SIMILAR,
                            };
                            cal_similar.SetMetaData();
                            NoteCRMRepository.Instance.Index(cal_similar);
                        }
                        catch (Exception ex)
                        {
                            _is_running_pharse_4 = false;
                            _logger.Error(ex);
                        }
                    }
                    else
                    {
                        _is_waiting_for_any_data = true;
                    }
                }
                catch (Exception ex)
                {
                    _is_running_pharse_4 = false;
                    _logger.Error(ex);
                }
                finally
                {
                    if (!_is_waiting_for_any_data)
                    {
                        _is_running_pharse_4 = false;
                        _is_running_pharse_2 = false;
                    }
                }
                #endregion
            }
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
        private static List<PharseContent> _calculateSimilary(int[] sum, long muc_tin)
        {
            List<PharseContent> data = new List<PharseContent>();
            double[] rate_avg = new double[muc_tin];
            try
            {
                Parallel.For(0, muc_tin, j =>
                {
                    rate_avg[j] = sum[j + muc_tin] == 0 ? 0 : (double)sum[j] / sum[j + muc_tin];
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }



            double[,] sim = new double[muc_tin, muc_tin];
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            int l = 0;
            try
            {
                for (int j = 0; j < muc_tin - 1; j++)
                {
                    for (int k = j + 1; k < muc_tin; k++)
                    {
                        sim[j, k] = sum[3 * muc_tin + l] / (Math.Sqrt(sum[2 * muc_tin + j]) * Math.Sqrt(sum[2 * muc_tin + k]));
                        l++;

                        PharseContent pharse = new PharseContent()
                        {
                            user_index = j,
                            key_index = k,
                            similary = sim[j, k],
                            pharse = Pharse.CALCULATE_SIMILAR,
                            rate_avg = rate_avg[j]
                        };
                        pharse.AutoId().SetMetaData();
                        data.Add(pharse);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return data;
        }
        private static double[] _calculateRateAvg(int[] sum, long muc_tin)
        {
            double[] rate_avg = new double[muc_tin];
            try
            {
                Parallel.For(0, muc_tin, j =>
                {
                    if (sum[j + muc_tin] == 0)
                    {
                        rate_avg[j] = 0;
                    }
                    else
                    {
                        rate_avg[j] = (double)sum[j] / sum[j + muc_tin];
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }


            return rate_avg;
        }



        private static async Task _sendRequest(string uri, object send_data)
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
