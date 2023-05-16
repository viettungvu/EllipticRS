using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EllipticModels;
using Nest;
using RSES;

namespace EllipticES
{
    public class TaiKhoanRepository : IESRepository
    {

        private static string _default_index = "";
        private static string _prefix_index = XMedia.XUtil.ConfigurationManager.AppSetting["PrefixIndex"];
        public TaiKhoanRepository(string modify_index)
        {
            _default_index = !string.IsNullOrEmpty(modify_index) ? modify_index : _default_index;
            ConnectionSettings settings = new ConnectionSettings(connectionPool, sourceSerializer: Nest.JsonNetSerializer.JsonNetSerializer.Default).DefaultIndex(_default_index).DisableDirectStreaming();
            settings.MaximumRetries(10);
            client = new ElasticClient(settings);
            var ping = client.Ping(p => p.Pretty(true));
            if (ping.ServerError != null && ping.ServerError.Error != null)
            {
                throw new Exception("START ES FIRST");
            }
        }
        private static TaiKhoanRepository _instance;
        public static TaiKhoanRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = _prefix_index + "_rs_tai_khoan";
                    _instance = new TaiKhoanRepository(_default_index);
                }
                return _instance;
            }
        }


        public bool Index(TaiKhoan tai_khoan)
        {
            if (tai_khoan != null)
            {
                return Index<TaiKhoan>(_default_index, tai_khoan, "", tai_khoan.id);
            }
            return false;
        }

        public bool IndexMany(IEnumerable<TaiKhoan> list_tai_khoan)
        {
            if (list_tai_khoan != null)
            {
                return IndexMany<TaiKhoan>(_default_index, list_tai_khoan);
            }
            return false;
        }

        public List<TaiKhoan> GetAll(out long total, int page = 1, int page_size = 99999, string[] view_field = null)
        {
            total = 0;
            List<TaiKhoan> dsach_tai_khoan = new List<TaiKhoan>()
            {

            };
            List<QueryContainer> filter = new List<QueryContainer>()
            {

            };
            SearchRequest req = new SearchRequest(_default_index)
            {
                Query = new QueryContainer(new BoolQuery { Filter = filter }),
            };
            req.ESCustomPaging(page, page_size);
            req.ESCustomSource(view_field);
            var res = client.Search<TaiKhoan>(req);
            if (res.IsValid)
            {
                total = res.Total;
                dsach_tai_khoan = res.Hits.Select(HitToDocument).ToList();
            }
            return dsach_tai_khoan;
        }

        public long CountTotal()
        {
            long total = 0;
            List<QueryContainer> filter = new List<QueryContainer>()
            {

            };
            SearchRequest req = new SearchRequest(_default_index)
            {
                Size = 0,
                Query = new QueryContainer(new BoolQuery { Filter = filter }),
                Aggregations = new CardinalityAggregation("CountTaiKhoan", "index")
            };
            var res = client.Search<Phim>(req);
            if (res.IsValid)
            {
                double total_dbl = res.Aggregations.Cardinality("CountTaiKhoan").Value ?? 0.0;
                total = (long)total_dbl;
            }
            return total;
        }

        public List<TaiKhoan> GetLastSuggestion(long last_sugguestion, int page, int page_size, out long total, string[] view_field = null)
        {
            total = 0;
            List<TaiKhoan> dsach_tai_khoan = new List<TaiKhoan>();
            List<QueryContainer> filter = new List<QueryContainer>();
            if (last_sugguestion > 0)
            {
                filter.Add(new LongRangeQuery { Field = "last_suggest_time", LessThanOrEqualTo = last_sugguestion });
            }
            SearchRequest req = new SearchRequest(_default_index)
            {
                Query = new QueryContainer(new BoolQuery { Filter = filter }),
            };
            req.ESCustomPaging(page, page_size);
            req.ESCustomSource(view_field);
            var res = client.Search<TaiKhoan>(req);
            if (res.IsValid)
            {
                total = res.Total;
                dsach_tai_khoan = res.Hits.Select(HitToDocument).ToList();
            }
            return dsach_tai_khoan;
        }
    }
}
