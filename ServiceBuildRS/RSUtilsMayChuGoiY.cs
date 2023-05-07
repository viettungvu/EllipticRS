using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public class RSUtilsMayChuGoiY
    {
        private static Curve _curve = new ECCBase16.Curve(ECCBase16.CurveName.secp160k1);
        private const int _max = 5;
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string _url_web_server = XMedia.XUtil.ConfigurationManager.AppSetting["UrlWebServer"];
        private static readonly string _url_api_receive = _url_web_server + "/api/recommend/receive-data";
        private static long users = 0;
        private static long movies = 0;
        private static bool _is_running_pharse_2 = false;
        private static bool _is_running_pharse_4 = false;
        private static bool _is_waiting_for_any_data = false;
        private static HashSet<string> _dsach_id_tai_khoan = new HashSet<string>();



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


            HashSet<string> hase = new HashSet<string>();
            if (!_is_running_pharse_2)
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    #region Pha 2 Tính các khóa công khai dùng chung Máy chủ thực hiện
                    try
                    {
                        List<PharseContent> khoa_cong_khai = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_SINH_KHOA_CONG_KHAI, out _);
                        if (khoa_cong_khai.Any())
                        {
                            _is_running_pharse_2 = true;
                            movies = khoa_cong_khai.First().total_movies;
                            users = khoa_cong_khai.GroupBy(x => x.user_id).Count();
                            long ns = movies * (movies + 5) / 2;
                            long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));

                            ConcurrentMultikeysDictionary<string, long, EiSiPoint> KPUij = new ConcurrentMultikeysDictionary<string, long, EiSiPoint>();

                            Parallel.ForEach(khoa_cong_khai, item =>
                            {
                                bool added = KPUij.TryAdd(item.user_id, item.key_index, PointPharseContent.ToEiSiPoint(item.point, _curve));
                                if (!added)
                                {
                                    throw new Exception();
                                }
                                _dsach_id_tai_khoan.Add(item.user_id);
                            });

                            ConcurrentBag<PharseContent> khoa_dung_chung = new ConcurrentBag<PharseContent>();
                            sw.Start();
                            Parallel.For(0, nk, j =>
                            {
                                EiSiPoint KPj = EiSiPoint.InfinityPoint;
                                foreach (string user_id in _dsach_id_tai_khoan)
                                {
                                    EiSiPoint tmp = KPj;
                                    KPj = EiSiPoint.Addition(tmp, KPUij[user_id, j]);
                                }
                                ECCBase16.AffinePoint sum = EiSiPoint.ToAffine(KPj);
                                concurrent_1.Add(string.Format("{0},{1}", j, sum.ToString()));
                                PharseContent user_key = new PharseContent()
                                {
                                    total_movies = movies,
                                    total_users = users,
                                    key_index = j,
                                    pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                                    point = PointPharseContent.Map(sum),
                                };
                                user_key.AutoId().SetMetaData();
                                khoa_dung_chung.Add(user_key);
                            });
                            sw.Stop();
                            WriteFile(_key_common, string.Join(Environment.NewLine, concurrent_1), false);
                            Clear(concurrent_1);
                            if (khoa_dung_chung.Any())
                            {
                                PharseContentRepository.Instance.IndexMany(khoa_dung_chung);
                            }
                            NoteCRM crm = new NoteCRM()
                            {
                                users = users,
                                news = movies,
                                time_complete = sw.ElapsedMilliseconds / 1000,
                                pharse = Pharse.BUILD_SINH_KHOA_DUNG_CHUNG,
                            };
                            crm.SetMetaData();
                            NoteCRMRepository.Instance.Index(crm);

                            IEnumerable<object> send_data = khoa_dung_chung.Select(x => new
                            {
                                x.key_index,
                                x.point,
                                x.pharse,
                                x.id,
                                x.user_id,
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
                        _is_running_pharse_2 = false;
                        _logger.Error(ex);
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    _is_running_pharse_2 = false;
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
                    List<PharseContent> ban_ma_xep_hang = PharseContentRepository.Instance.GetByPharse(Pharse.BUILD_MA_HOA_XEP_HANG, out _);
                    if (ban_ma_xep_hang.Any())
                    {
                        _is_running_pharse_4 = true;
                        long ns = movies * (movies + 5) / 2;
                        long nk = (int)Math.Ceiling(0.5 + Math.Sqrt(ns * 2 + 0.25));

                        ConcurrentMultikeysDictionary<string, long, AffinePoint> AUij = new ConcurrentMultikeysDictionary<string, long, AffinePoint>();
                        Parallel.ForEach(ban_ma_xep_hang, aui =>
                        {
                            AUij.TryAdd(aui.user_id, aui.key_index, PointPharseContent.ToAffinePoint(aui.point, _curve));
                        });
                        ConcurrentBag<PharseContent> tong_bao_mat = new ConcurrentBag<PharseContent>();
                        Parallel.For(0, ns, (j) =>
                        {
                            EiSiPoint sum = EiSiPoint.InfinityPoint;
                            foreach (string user_id in _dsach_id_tai_khoan)
                            {
                                EiSiPoint tmp = sum;
                                sum = EiSiPoint.Addition(tmp, AffinePoint.ToEiSiPoint(AUij[user_id, j]));
                            }
                            AffinePoint affine = EiSiPoint.ToAffine(sum);
                            concurrent_1.Add(string.Format("{0},{1}", j, affine.ToString()));
                            PharseContent pharse_4 = new PharseContent
                            {
                                key_index = j,
                                point = PointPharseContent.Map(affine),
                                pharse = Pharse.BUILD_TINH_TONG_BAO_MAT,
                            };
                            pharse_4.AutoId().SetMetaData();
                            tong_bao_mat.Add(pharse_4);
                        });
                        sw.Stop();
                        WriteFile(_sum_encrypt, string.Join(Environment.NewLine, concurrent_1), false);
                        Clear(concurrent_1);
                        if (tong_bao_mat.Any())
                        {
                            PharseContentRepository.Instance.IndexMany(tong_bao_mat);
                        }
                        NoteCRM crm = new NoteCRM()
                        {
                            users = users,
                            news = movies,
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
                            Parallel.ForEach(tong_bao_mat, aj =>
                            {
                                Aj[aj.key_index] = PointPharseContent.ToAffinePoint(aj.point, _curve);
                            });
                            int[] data_loga = _brfStandard(Aj, ns, _max * _max * users);
                            WriteFile(_get_sum_encrypt, string.Join(";", data_loga), false);
                            List<PharseContent> similary = _calculateSimilary(data_loga, movies);
                            PharseContentRepository.Instance.IndexMany(similary);
                            sw.Stop();
                            NoteCRM cal_similar = new NoteCRM()
                            {
                                users = users,
                                news = movies,
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
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            ConcurrentBag<string> bag_round = new ConcurrentBag<string>();
            double[] rate_avg = new double[muc_tin];
            try
            {
                Parallel.For(0, muc_tin, j =>
                {
                    rate_avg[j] = sum[j + muc_tin] == 0 ? 0 : (double)sum[j] / sum[j + muc_tin];
                    bag.Add(string.Format("{0},{1}", j, rate_avg[j]));
                });
                WriteFile(_rate_avg, String.Join(Environment.NewLine, bag), false);
                Clear(bag);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }



            double[,] sim = new double[muc_tin, muc_tin];
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
                        PharseContent pharse = new PharseContent()
                        {
                            user_id=j.ToString(),
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
                WriteFile(_sim, String.Join(Environment.NewLine, bag), false);
                WriteFile(_sim_round, String.Join(Environment.NewLine, bag_round), false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return data;
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
    }
}
