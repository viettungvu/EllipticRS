using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EllipticES;
using EllipticModels;
using Nest;

namespace RSES
{
    public class NoteCRMRepository : IESRepository
    {
        private static string _default_index = "";
        public NoteCRMRepository(string modify_index)
        {
            _default_index = !string.IsNullOrEmpty(modify_index) ? modify_index : _default_index;
            ConnectionSettings settings = new ConnectionSettings(connectionPool, sourceSerializer: Nest.JsonNetSerializer.JsonNetSerializer.Default).DefaultIndex(_default_index).DisableDirectStreaming(true);
            settings.MaximumRetries(10);
            client = new ElasticClient(settings);
            var ping = client.Ping(p => p.Pretty(true));
            if (ping.ServerError != null && ping.ServerError.Error != null)
            {
                throw new Exception("START ES FIRST");
            }
        }
        private static NoteCRMRepository _instance;
        public static NoteCRMRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = "rs_log";
                    _instance = new NoteCRMRepository("rs_log");
                }
                return _instance;
            }
        }

        public bool Index(NoteCRM data)
        {
            return false;
            //return Index<NoteCRM>(_default_index, data, "", data.id);
        }

        public List<NoteCRM> GetAll(int thuoc_tinh)
        {
            List<QueryContainer> filter = new List<QueryContainer>()
            {
                new TermQuery{Field="thuoc_tinh", Value=thuoc_tinh}
            };
            SearchRequest req = new SearchRequest(_default_index)
            {
                Size = 1000,
                Query = new QueryContainer(new BoolQuery { Filter = filter }),
            };
            var res = client.Search<NoteCRM>(req);
            if (res.IsValid)
            {
                return res.Hits.Select(HitToDocument).ToList();
            }
            return new List<NoteCRM>();
        }
    }
}
