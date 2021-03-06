﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nest;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    public class ElasticSearchResults<T> : ISearchResults<T> where T : class
    {
        public ElasticSearchResults(ISearchCriteria criteria, ISearchResponse<T> response)
        {
            SearchCriteria = criteria;
            Documents = response.Documents?.ToList();
            DocCount = response.HitsMetaData.Hits.Count;
            TotalCount = response.Total;
            ProviderAggregations = response.Aggregations;
            Facets = CreateFacets(criteria, response.Aggregations);
        }

        public IReadOnlyDictionary<string, IAggregate> ProviderAggregations
        {
            get;
        }

        public IList<FacetGroup> Facets { get; }

        public long DocCount { get; }

        public IList<T> Documents { get; }

        public ISearchCriteria SearchCriteria { get; }

        public long TotalCount { get; }

        public IList<string> Suggestions { get; }

        private static FacetGroup[] CreateFacets(ISearchCriteria criteria, IReadOnlyDictionary<string, IAggregate> facets)
        {
            var result = new List<FacetGroup>();

            if (facets != null)
            {
                foreach (var filter in criteria.Filters)
                {
                    var groupLabels = filter.GetLabels();
                    var facetGroup = new FacetGroup(filter.Key, groupLabels);

                    var values = filter.GetValues();

                    // Return all facet terms for attribute filter if values are not defined
                    if (values == null && filter is AttributeFilter)
                    {
                        facetGroup.FacetType = FacetTypes.Attribute;

                        var key = filter.Key.ToLowerInvariant();
                        if (facets.ContainsKey(key))
                        {
                            var facet = facets[key] as SingleBucketAggregate;
                            var termAgg = facet?.Aggregations?[key] as BucketAggregate;
                            if (termAgg != null)
                            {
                                foreach (var term in termAgg.Items.OfType<KeyedBucket<object>>())
                                {
                                    var newFacet = new Facet(facetGroup, ToStringInvariant(term.Key), term.DocCount, null);
                                    facetGroup.Facets.Add(newFacet);
                                }
                            }
                        }
                    }

                    if (values != null)
                    {
                        foreach (var group in values.GroupBy(v => v.Id))
                        {
                            var valueLabels = group.GetValueLabels();

                            if (filter is AttributeFilter)
                            {
                                facetGroup.FacetType = FacetTypes.Attribute;

                                var key = filter.Key.ToLowerInvariant();
                                if (facets.ContainsKey(key))
                                {
                                    var facet = facets[key] as SingleBucketAggregate;
                                    var termAgg = facet?.Aggregations?[key] as BucketAggregate;
                                    var term = termAgg?.Items.OfType<KeyedBucket<object>>().FirstOrDefault(t => ToStringInvariant(t.Key).EqualsInvariant(group.Key));
                                    if (term != null)
                                    {
                                        var newFacet = new Facet(facetGroup, group.Key, term.DocCount, valueLabels);
                                        facetGroup.Facets.Add(newFacet);
                                    }
                                }
                            }
                            else if (filter is PriceRangeFilter)
                            {
                                facetGroup.FacetType = FacetTypes.PriceRange;

                                var rangeFilter = filter as PriceRangeFilter;
                                if (rangeFilter.Currency.EqualsInvariant(criteria.Currency))
                                {
                                    var key = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", filter.Key, group.Key).ToLowerInvariant();
                                    if (facets.ContainsKey(key))
                                    {
                                        var facet = facets[key] as SingleBucketAggregate;
                                        if (facet != null && facet.DocCount > 0)
                                        {
                                            var newFacet = new Facet(facetGroup, group.Key, facet.DocCount, valueLabels);
                                            facetGroup.Facets.Add(newFacet);
                                        }
                                    }
                                }
                            }
                            else if (filter is RangeFilter)
                            {
                                facetGroup.FacetType = FacetTypes.Range;

                                var key = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", filter.Key, group.Key).ToLowerInvariant();
                                if (facets.ContainsKey(key))
                                {
                                    var facet = facets[key] as SingleBucketAggregate;
                                    if (facet != null && facet.DocCount > 0)
                                    {

                                        var newFacet = new Facet(facetGroup, group.Key, facet.DocCount, valueLabels);
                                        facetGroup.Facets.Add(newFacet);
                                    }
                                }
                            }
                            else if (filter is CategoryFilter)
                            {
                                facetGroup.FacetType = FacetTypes.Category;

                                var key = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", filter.Key, group.Key).ToLowerInvariant();
                                if (facets.ContainsKey(key))
                                {
                                    var facet = facets[key] as SingleBucketAggregate;
                                    if (facet != null && facet.DocCount > 0)
                                    {
                                        var newFacet = new Facet(facetGroup, group.Key, (int)facet.DocCount, valueLabels);
                                        facetGroup.Facets.Add(newFacet);
                                    }
                                }
                            }
                        }
                    }

                    // Add facet group only if has items
                    if (facetGroup.Facets.Any())
                    {
                        result.Add(facetGroup);
                    }
                }
            }

            return result.ToArray();
        }

        private static string ToStringInvariant(object value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }
    }
}
