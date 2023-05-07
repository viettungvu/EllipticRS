using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EllipticModels;
using Nest;
using RSES;

namespace EllipticES
{
    public class TheLoaiPhimRepository : IESRepository
    {

        private static string _default_index = "";
        private static string _prefix_index = XMedia.XUtil.ConfigurationManager.AppSetting["PrefixIndex"];
        public TheLoaiPhimRepository(string modify_index)
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
        private static TheLoaiPhimRepository _instance;
        public static TheLoaiPhimRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = _prefix_index + "_rs_phim";
                    _instance = new TheLoaiPhimRepository(_default_index);
                }
                return _instance;
            }
        }


        public bool Index(Phim muc_tin)
        {
            if (muc_tin != null)
            {
                return Index<Phim>(_default_index, muc_tin, "", muc_tin.id);
            }
            return false;
        }

        public bool IndexMany(IEnumerable<Phim> list_muc_tin)
        {
            if (list_muc_tin != null)
            {
                return IndexMany<Phim>(_default_index, list_muc_tin);
            }
            return false;
        }

        public List<Phim> GetAll(out long total, int page = 1, int page_size = 99999, string[] view_field = null)
        {
            total = 0;
            List<Phim> dsach_phim = new List<Phim>()
            {

            };
            List<QueryContainer> filter = new List<QueryContainer>()
            {
                new TermQuery{Field="loai", Value=LoaiPhim.THE_LOAI_PHIM}
            };
            SearchRequest req = new SearchRequest(_default_index)
            {
                Query = new QueryContainer(new BoolQuery { Filter = filter }),
            };
            req.ESCustomPaging(page, page_size);
            req.ESCustomSource(view_field);
            var res = client.Search<Phim>(req);
            if (res.IsValid)
            {
                total = res.Total;
                dsach_phim = res.Hits.Select(HitToDocument).ToList();
            }
            return dsach_phim;
        }
    }
}
