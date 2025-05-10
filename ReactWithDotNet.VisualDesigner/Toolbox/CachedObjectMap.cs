using System.Collections.Concurrent;

namespace ReactWithDotNet.VisualDesigner.Toolbox;

public sealed class CachedObjectMap
{
    readonly ConcurrentDictionary<string, CacheItem> map = new();

    public TimeSpan Timeout { get; init; } = TimeSpan.MaxValue;

    public async Task<T> AccessValue<T>(string key, Func<Task<T>> createFunc) where T : class
    {
        if (map.TryGetValue(key, out var record))
        {
            if (DateTime.Now - record.CreationTime <= Timeout)
            {
                return (T)record.Value;
            }
        }

        var value = await createFunc();
        if (value is null)
        {
            return null;
        }

        map[key] = record = new() { CreationTime = DateTime.Now, Value = value };

        return (T)record.Value;
    }
    
    public T AccessValue<T>(string key, Func<T> createFunc) where T : class
    {
        if (map.TryGetValue(key, out var record))
        {
            if (DateTime.Now - record.CreationTime <= Timeout)
            {
                return (T)record.Value;
            }
        }

        var value = createFunc();
        if (value is null)
        {
            return null;
        }

        map[key] = record = new() { CreationTime = DateTime.Now, Value = value };

        return (T)record.Value;
    }

    public void Clear()
    {
        map.Clear();
    }

    sealed class CacheItem
    {
        public DateTime CreationTime { get; init; }

        public object Value { get; init; }
    }
}