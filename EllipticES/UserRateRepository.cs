using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EllipticModels;
using Nest;
using RSES;

namespace EllipticES
{
    public class UserRateRepository : IESRepository
    {

        private static string _default_index = "";
        public UserRateRepository(string modify_index)
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
        private static UserRateRepository _instance;
        public static UserRateRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = "rs_user_rate";
                    _instance = new UserRateRepository(_default_index);
                }
                return _instance;
            }
        }


        public bool Index(UserRate user_rate)
        {
            if (user_rate != null)
            {
                return Index<UserRate>(_default_index, user_rate, "", user_rate.id);
            }
            return false;
        }

        public bool IndexMany(IEnumerable<UserRate> list_user_rate)
        {
            if (list_user_rate == null)
            {
                return IndexMany<UserRate>(_default_index, list_user_rate);
            }
            return false;
        }

        public List<UserRate> GetAll(out long total, string username = "", long user_index = -1, long news_index = -1, int page = 1, int page_size = 99999, string[] view_field = null)
        {
            total = 0;
            List<UserRate> user_rates = new List<UserRate>();
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
            var res = client.Search<UserRate>(req);
            if (res.IsValid)
            {
                total = res.Total;
                user_rates = res.Documents.ToList();
            }
            return user_rates;
        }
    }
}
