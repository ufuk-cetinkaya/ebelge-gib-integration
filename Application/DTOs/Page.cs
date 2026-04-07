
namespace Application.DTOs;

public class Page<T>
{
    private readonly int _totalRecords;
    private readonly int _pageSize;
    private readonly int _pageCount;
    private bool _hasPreviousPage;
    private bool _hasNextPage;
    private bool _isFirstPage;
    private bool _isLastPage;
    private int _skip;
    private int _fetch;
    private int _currentPage;
    public IEnumerable<T>? _data;

    public Page(int totalRecords, int pageSize, int currentPage)
    {
        // Sayfa sayısının negatif olmaması için
        _totalRecords = Math.Max(totalRecords, 0);
        // Sıfıra bölme hatası almaması ve sayfa sayısının negatif olmaması için
        _pageSize = Math.Max(pageSize, 1);
        _pageCount = Convert.ToInt32(Math.Ceiling(decimal.Divide(_totalRecords, _pageSize)));
        _currentPage = currentPage;
        SetParams();
    }

    public int TotalRecords => _totalRecords;
    public int PageCount => _pageCount;
    public bool HasPreviousPage => _hasPreviousPage;
    public bool HasNextPage => _hasNextPage;
    public bool IsFirstPage => _isFirstPage;
    public bool IsLastPage => _isLastPage;
    public int Skip => _skip;
    public int Fetch => _fetch;

    public IEnumerable<T>? Data { get; set; }

    public void First()
    {
        if (!_isFirstPage)
        {
            _currentPage = 1;
            SetParams();
        }
    }

    public void Last()
    {
        if (!_isLastPage)
        {
            _currentPage = _pageCount;
            SetParams();
        }
    }

    public void Previous()
    {
        if (_hasPreviousPage)
        {
            _currentPage--;
            SetParams();
        }
    }

    public void Next()
    {
        if (_hasNextPage)
        {
            _currentPage++;
            SetParams();
        }
    }

    private void SetParams()
    {
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
}
