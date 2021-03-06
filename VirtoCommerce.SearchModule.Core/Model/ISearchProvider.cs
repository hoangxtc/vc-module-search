﻿using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public interface ISearchProvider
    {
        ISearchQueryBuilder[] QueryBuilders { get; }

        void Close(string scope, string documentType);

        void Commit(string scope);

        void Index<T>(string scope, string documentType, T document);

        int Remove(string scope, string documentType, string key, string value);

        bool RemoveAll(string scope, string documentType);

        ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class;
    }
}
