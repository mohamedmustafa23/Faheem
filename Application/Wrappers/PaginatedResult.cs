namespace Application.Wrappers
{
    public class PaginatedResult<T>
    {
        public List<T> Data { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;

        public static PaginatedResult<T> Create(List<T> data, int totalCount, int page, int pageSize)
            => new() { Data = data, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }
}
