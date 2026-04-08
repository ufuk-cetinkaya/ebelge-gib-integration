
namespace Application.DTOs;

public class Page<T>
{
    private readonly int _totalRecords;
    private readonly int _pageSize;
    private readonly int _pageCount;
    private readonly bool _hasPreviousPage;
    private readonly bool _hasNextPage;
    private readonly bool _isFirstPage;
    private readonly bool _isLastPage;
    private readonly int _skip;
    private readonly int _fetch;
    private readonly int _currentPage;

    public Page(int totalRecords, int pageSize, int currentPage)
    {
        _totalRecords = Math.Max(totalRecords, 0);
        _pageSize = Math.Max(pageSize, 1);
        _pageCount = Convert.ToInt32(Math.Ceiling(decimal.Divide(_totalRecords, _pageSize)));
        _currentPage = currentPage;

        if (_pageCount == 0)
        {
            _hasPreviousPage = false;
            _hasNextPage = false;
            _isFirstPage = false;
            _isLastPage = false;
        }
        else
        {
            _hasPreviousPage = _currentPage > 1;
            _hasNextPage = _currentPage < _pageCount;
            _isFirstPage = _currentPage == 1;
            _isLastPage = _currentPage == _pageCount;
        }

        if (_currentPage > 2) _skip = (_currentPage - 1) * _pageSize;
        else if (_currentPage == 2) _skip = _pageSize;
        else _skip = 0;

        _fetch = Math.Max((_currentPage * _pageSize) - _skip, 0);
    }

    public int TotalRecords => _totalRecords;
    public int PageCount => _pageCount;
    public bool HasPreviousPage => _hasPreviousPage;
    public bool HasNextPage => _hasNextPage;
    public bool IsFirstPage => _isFirstPage;
    public bool IsLastPage => _isLastPage;
    internal int Skip => _skip;
    internal int Fetch => _fetch;
    public IEnumerable<T>? Data { get; set; }
}
