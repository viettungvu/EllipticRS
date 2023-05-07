using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EllipticModels;
using Nest;
using RSES;

namespace EllipticES
{
    public class UserRateV2Repository : IESRepository
    {

        private static string _default_index = "";
        private static string _prefix_index= XMedia.XUtil.ConfigurationManager.AppSetting["PrefixIndex"];
        public UserRateV2Repository(string modify_index)
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
        private static UserRateV2Repository _instance;
        public static UserRateV2Repository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = _prefix_index+"_rs_user_rate";
                    _instance = new UserRateV2Repository(_default_index);
                }
                return _instance;
            }
        }


        public bool Index(UserRateV2 user_rate)
        {
            if (user_rate != null)
            {
                return Index<UserRateV2>(_default_index, user_rate, "", user_rate.id);
            }
            return false;
        }

        public bool IndexMany(IEnumerable<UserRateV2> list_user_rate)
        {
            if (list_user_rate != null)
            {
                return IndexMany<UserRateV2>(_default_index, list_user_rate);
            }
            return false;
        }

        public List<UserRateV2> GetAll(out long total, string[] view_field = null, int page = 1, int page_size = 99999)
        {
            total = 0;
            List<UserRateV2> user_rates = new List<UserRateV2>();
            List<QueryContainer> filter = new List<QueryContainer>();
            SearchRequest req = new SearchRequest(_default_index)
            {
                Query = new QueryContainer(new BoolQuery { Filter = filter }),

            };
            req.ESCustomPaging(page, page_size);
            req.ESCustomSource(view_field);
            var res = client.Search<UserRateV2>(req);
            if (res.IsValid)
            {
                total = res.Total;
                user_rates = res.Documents.ToList();
            }
            return user_rates;
        }
    }
}
