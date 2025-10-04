namespace ReactWithDotNet.VisualDesigner.FunctionalUtilities;

public sealed record Result<TValue>
{
    public Exception Error { get; init; }

    public bool HasError => !Success;

    public bool Success { get; init; }

    public TValue Value { get; init; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new() { Value = value, Success = true };
    }

    public static implicit operator Result<TValue>(Exception failInfo)
    {
        return new() { Error = failInfo };
    }

    public static implicit operator Result<TValue>(NoneObject _)
    {
        return new() { Success = true };
    }

    public static implicit operator Result<TValue>(Result<Unit> value)
    {
        return new() { Success = value.Success, Error = value.Error };
    }
}