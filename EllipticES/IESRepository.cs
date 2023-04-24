using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Elasticsearch.Net;
using EllipticModels;
using Nest;

namespace EllipticES
{
    public class IESRepository
    {

        protected static ElasticClient client;
        protected static Uri node = new Uri(System.Configuration.ConfigurationManager.AppSettings["ES:Server"]);
        protected static StickyConnectionPool connectionPool = new StickyConnectionPool(new[] { node });
        protected static long max_result_windown = 99999;
        protected bool Index<T>(string _defaultIndex, T data, string route, string id = "") where T : class
        {
            IndexRequest<object> req = new IndexRequest<object>(_defaultIndex, typeof(T));
            if (!string.IsNullOrEmpty(route))
                req.Routing = route;
            req.Document = data;
            IndexResponse re = null;
            if (!string.IsNullOrEmpty(id))
                re = client.Index<T>(data, i => i.Id(id));
            else
                re = client.Index(req);

            try
            {
                Type t = data.GetType();
                PropertyInfo piShared = t.GetProperty("id");
                if (piShared != null)
                    piShared.SetValue(data, re.Id);
            }
            catch (Exception ex)
            { }
            return re.Result == Result.Created;
        }

        public bool IndexMany<T>(string default_index, IEnumerable<T> data) where T : class
        {
            var res = client.Bulk(b => b.IndexMany<T>(data, (bu, doc) => bu.Index(default_index).Document(doc)));
            return res.IsValid && !res.Errors;
        }


        protected bool Delete<T>(string default_index, string id) where T : class
        {
            var res = client.Delete<T>(id, x => x.Index(default_index));
            return res.IsValid && res.Result == Result.Deleted;
        }

        protected bool Update<T>(string default_index, string id, object data) where T : class
        {
            var res = client.Update<T, object>(id, u => u.Index(default_index).Doc(data));
            return res.IsValid && (res.Result == Result.Updated || res.Result == Result.Noop);
        }

        protected T GetById<T>(string default_index, string id) where T : class
        {
            var res = client.Get<T>(id, g => g.Index(default_index));
            if (res.Found)
            {
                return res.Source;
            }
            return default;
        }
        public List<T> GetAll<T>(string default_index, string[] view_field = null) where T : class
        {
            var res = client.Search<T>(s => s.Query(q => q.MatchAll()).Size(99999).Index(default_index));
            if (res.IsValid)
            {
                return res.Hits.Select(x => x.Source).ToList();
            }
            return new List<T>();
        }
    }

    public static class IESExtension
    {
        public static SearchRequest ESCustomPaging(this SearchRequest request, int page, int page_size)
        {
            if (page < 1)
            {
                page = 1;
            }
            if (page_size < 0)
            {
                page_size = 100;
            }
            request.From = (page - 1) * page_size;
            request.Size = page_size;
            return request;
        }

        public static SearchRequest ESCustomSource(this SearchRequest request, string[] includes = null, string[] excludes = null)
        {
            SourceFilter source = new SourceFilter { };
            if (includes != null && !includes.Contains("*"))
            {
                source.Includes = includes;
            }

            if (excludes == null || !excludes.Contains("*"))
            {
                source.Excludes = excludes;
            }
            request.Source = source;
            return request;
        }

        public static SearchRequest ESCustomSort(this SearchRequest request, Dictionary<string, CustomSort> dic_sort = null)
        {
            if (dic_sort != null && dic_sort.Any())
            {
                List<ISort> sort = new List<ISort>();
                foreach (var item in dic_sort)
                {
                    sort.Add(new FieldSort { Field = item.Key, Order = item.Value == CustomSort.ASC ? SortOrder.Ascending : SortOrder.Descending });
                }
                request.Sort = sort;
            }
            return request;
        }
    }

}
