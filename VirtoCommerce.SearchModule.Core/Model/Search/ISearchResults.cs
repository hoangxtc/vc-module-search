﻿using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public interface ISearchResults<T> where T : class
    {
        IList<T> Documents { get; }

        ISearchCriteria SearchCriteria { get; }

        IList<FacetGroup> Facets { get; }

        IList<string> Suggestions { get; }

        long DocCount { get; }

        long TotalCount { get; }
    }
}
