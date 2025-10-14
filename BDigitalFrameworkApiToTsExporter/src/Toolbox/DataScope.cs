namespace Toolbox;

public sealed class DataKey<T>
{
    public required string Key { get; init; }

    public T? this[DataScope scope]
    {
        get => scope.GetData(this);
        set => scope.SetData(this, value);
    }
}

public sealed class DataScope
{
    readonly Dictionary<string, object?> store = new();

    public T? GetData<T>(DataKey<T> key)
    {
        if (store.TryGetValue(key.Key, out var value))
        {
            return (T?)value;
        }

        return default;
    }

    public void SetData<T>(DataKey<T> key, T? value)
    {
        store[key.Key] = value;
    }
}