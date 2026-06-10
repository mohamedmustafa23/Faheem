namespace Application.Wrappers
{
    public class Result
    {
        public bool Succeeded { get; init; }
        public string? Message { get; init; }
        public List<string> Errors { get; init; } = new();

        public static Result Success(string? message = null)
            => new() { Succeeded = true, Message = message };

        public static Result Failure(params string[] errors)
            => new() { Succeeded = false, Errors = errors.ToList() };
    }

    public class Result<T> : Result
    {
        public T? Data { get; init; }

        public static Result<T> Success(T data, string? message = null)
            => new() { Succeeded = true, Message = message, Data = data };

        public static new Result<T> Failure(params string[] errors)
            => new() { Succeeded = false, Errors = errors.ToList() };
    }
}