using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class KendoGrid
    {
        public int Id { get; set; }
        public string EmployeeID { set; get; }
        public string Type { get; set; }
        public int Take { set; get; }
        public int Skip { set; get; }
        public int Page { set; get; }
        public int PageSize { set; get; }
        public KendoFilters Filter { set; get; }
        public List<KendoSort> Sort { set; get; } = new List<KendoSort>();

    }

    public class KendoFilters
    {
        public List<KendoFilter> Filters { set; get; } = new List<KendoFilter>();
        public string Logic { set; get; }
    }

    public class KendoFilter {
        public string Field { set; get; }
        public string Operator { set; get; }
        public string Value { set; get; }
    }

    public class KendoSort
    {
        public string Field { set; get; }
        public string Dir { set; get; }
    }

    public static class KendoMongoBuilder<T> {
        public static FilterDefinition<T> BuildFilter(KendoGrid param) {
            var filters = new List<FilterDefinition<T>>();
            var filterBuilder = Builders<T>.Filter;            
            
            if (param != null && param.Filter != null && param.Filter.Filters.Count > 0)
            {
                foreach (var filter in param.Filter.Filters)
                {
                    switch (filter.Operator)
                    {
                        case "eq":
                            filters.Add(filterBuilder.Eq(filter.Field, filter.Value));
                            break;
                        case "startswith":                            
                            var a = new BsonRegularExpression($"^{filter.Value}","i");                           
                            filters.Add(filterBuilder.Regex(filter.Field, a));
                            break;
                        case "contains":
                            var contains = new BsonRegularExpression($"{filter.Value}", "i");
                            filters.Add(filterBuilder.Regex(filter.Field, contains));
                            break;
                        case "doesnotcontain":
                            var regexNotContains = new Regex($"/^((?!${filter.Value}).)*$/");
                            filters.Add(filterBuilder.Regex(filter.Field, regexNotContains));
                            break;
                        default:
                            break;
                    }

                }

                if (param.Filter.Logic == "and")
                {
                    return filterBuilder.And(filters);
                }
                
                return filterBuilder.Or(filters);
            }
            
            return filterBuilder.Empty;

        }

        public static SortDefinition<T> BuildSort(KendoGrid param)
        {
            var sorters = new List<SortDefinition<T>>();
            var sortBuilder = Builders<T>.Sort;

            if (param != null && param.Sort != null && param.Sort.Count > 0)
            {
                foreach (var sort in param.Sort)
                {
                    switch (sort.Dir)
                    {
                        case "asc":
                            sorters.Add(sortBuilder.Ascending(sort.Field));
                            break;
                        case "desc":
                            sorters.Add(sortBuilder.Descending(sort.Field));
                            break;
                        default:
                            break;
                    }

                }
                return sortBuilder.Combine(sorters);
            }

            return null;
        }
    }
}
