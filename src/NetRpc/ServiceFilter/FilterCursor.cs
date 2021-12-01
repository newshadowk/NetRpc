using System.Collections.Generic;

namespace NetRpc;

public class FilterCursor
{
    private readonly List<IAsyncActionFilter> _filters;
    private int _index;

    public FilterCursor(List<IAsyncActionFilter> filters)
    {
        _filters = filters;
        _index = 0;
    }

    public void Reset()
    {
        _index = 0;
    }

    public IAsyncActionFilter? GetNextFilter()
    {
        while (_index < _filters.Count)
        {
            var filterAsync = _filters[_index];
            _index += 1;
            return filterAsync;
        }

        return default;
    }
}