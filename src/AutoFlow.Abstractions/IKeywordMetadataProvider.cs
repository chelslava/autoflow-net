// Этот код нужен для получения metadata по зарегистрированным keyword.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public interface IKeywordMetadataProvider
{
    IReadOnlyCollection<KeywordMetadata> GetKeywords();
}
