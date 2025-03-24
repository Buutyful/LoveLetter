namespace LoveLetter.Common;

public record Error(string Message);
public class Result<T>
{
    private Result(){}
    public T? Value { get; private set; }
    public Error? Error { get; private set; }
    public bool IsSuccess => Value is not null;
    public static Result<T> Success(T value) =>
        new Result<T> { Value = value };
    public static Result<T> Failure(Error error) => 
        new Result<T> { Error = error };
    public T Reduce(T orElse) => Value??= orElse;

    public Result<Tout> Map<Tout>(Func<T, Tout> map) =>
        Value is null ?
        Result<Tout>.Failure(Error!) :
        Result<Tout>.Success(map(Value));
}
