using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EllipticModels;
using Nest;
using RSES;

namespace EllipticES
{
    public class PharseContentRepository : IESRepository
    {

        private static string _default_index = "";
        public PharseContentRepository(string modify_index)
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
        private static PharseContentRepository _instance;
        public static PharseContentRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = "rs_user_key";
                    _instance = new PharseContentRepository(_default_index);
                }
                return _instance;
            }
        }


        public bool Index(PharseContent user_key)
        {
            if (user_key != null)
            {
                return Index<PharseContent>(_default_index, user_key, "", user_key.id);
            }
            return false;
        }

        public bool IndexMany(IEnumerable<PharseContent> list_user_rate)
        {
            if (list_user_rate == null)
            {
                return IndexMany<PharseContent>(_default_index, list_user_rate);
            }
            return false;
        }

        public List<PharseContent> GetAll(out long total, string username = "", long user_index = -1, long news_index = -1, int page = 1, int page_size = 99999, string[] view_field = null)
        {
            total = 0;
            List<PharseContent> user_keys = new List<PharseContent>();
            List<QueryContainer> filter = new List<QueryContainer>();
            if (!string.IsNullOrWhiteSpace(username))
            {
                filter.Add(new TermQuery { Field = "username.keyword", Value = username });
            }
            if (user_index > -1)
            {
                filter.Add(new TermQuery { Field = "user_index", Value = user_index });
            }
            if (news_index > -1)
            {
                filter.Add(new TermQuery { Field = "news_index", Value = news_index });
            }
            SearchRequest req = new SearchRequest(_default_index)
            {
                Query = new QueryContainer(new BoolQuery { Filter = filter }),

            };
            req.ESCustomPaging(page, page_size);
            req.ESCustomSource(view_field);
            var res = client.Search<PharseContent>(req);
            if (res.IsValid)
            {
                total = res.Total;
                user_keys = res.Documents.ToList();
            }
            return user_keys;
        }


        public List<PharseContent> GetByPharse(Pharse pharse, out long total, int page = 1, int page_size = 99999, string[] view_field = null)
        {
            total = 0;
            List<PharseContent> pharse_content = new List<PharseContent>();
            List<QueryContainer> filter = new List<QueryContainer>();
            if (pharse != Pharse.ALL)
            {
                filter.Add(new TermQuery { Field = "pharse", Value = pharse });
            }
            SearchRequest req = new SearchRequest(_default_index)
            {
                Query = new QueryContainer(new BoolQuery { Filter = filter }),

            };
            req.ESCustomPaging(page, page_size);
            req.ESCustomSource(view_field);
            var res = client.Search<PharseContent>(req);
            if (res.IsValid)
            {
                total = res.Total;
                pharse_content = res.Documents.ToList();
            }
            return pharse_content;
        }
    }
}
