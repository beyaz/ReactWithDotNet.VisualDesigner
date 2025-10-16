using System.Collections;
using System.Diagnostics;

namespace Toolbox;

public class ScopeKey
{
    public required string Key { get; init; }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}

public sealed class ScopeKey<T> : ScopeKey
{
    public T this[Scope scope]
    {
        get
        {
            var map = scope.AsDictionary();
            if (map.TryGetValue(this, out var value))
            {
                return (T)value;
            }

            throw new KeyNotFoundException(Key);
        }
    }
}

public sealed class ScopeCreationInput : IEnumerable
{
    readonly Dictionary<ScopeKey, object> items = new();

    public void Add<T>(ScopeKey<T> key, T value)
    {
        items.Add(key, value!);
    }

    public IReadOnlyDictionary<ScopeKey, object> AsDictionary() => items;

    public IEnumerator GetEnumerator()
    {
        return items.GetEnumerator();
    }
}

[DebuggerTypeProxy(typeof(ScopeDebugView))]
public sealed class Scope
{
    readonly Dictionary<ScopeKey, object> items = new();

    Scope()
    {
    }

    public static Scope Create(ScopeCreationInput input)
    {
        var scope = new Scope();

        foreach (var (key, value) in input.AsDictionary())
        {
            scope.items.Add(key, value);
        }

        return scope;
    }

    public IReadOnlyDictionary<ScopeKey, object> AsDictionary() => items;

    public bool Has(ScopeKey key)
    {
        return items.ContainsKey(key);
    }

    public Scope With(ScopeCreationInput input)
    {
        var scope = new Scope();

        foreach (var (key, value) in items)
        {
            scope.items.Add(key, value);
        }

        foreach (var (key, value) in input.AsDictionary())
        {
            scope.items[key] = value;
        }

        return scope;
    }
    
    public Scope With<T>(ScopeKey<T> scopeKey, T value )
    {
        var scope = new Scope();

        foreach (var item in items)
        {
            scope.items.Add(item.Key, item.Value);
        }

        scope.items[scopeKey] = value!;

        return scope;
    }
    
    

    

    class ScopeDebugView
    {
        readonly Scope scope;

        public ScopeDebugView(Scope scope)
        {
            this.scope = scope;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ItemDebugView[] Items => scope.items.Select(p => new ItemDebugView { Key = p.Key.Key, Value = p.Value }).ToArray();
    }

    [DebuggerDisplay("{Key} : {Value}")]
    class ItemDebugView
    {
        public required string Key { get; init; }
        public required object Value { get; init; }
    }
}

public static class ScopeExtensions
{
    public static Result<Scope> With<T>(this Scope existingScope, ScopeKey<T> scopeKey, Result<T> result)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return existingScope.With(scopeKey, result.Value);
    }
    public static Result<Scope> With<T>(this Result<Scope> existingScope, ScopeKey<T> scopeKey, Func<Scope, Result<T>> next)
    {
        if (existingScope.HasError)
        {
            return existingScope.Error;
        }

        var result = next(existingScope.Value);
        
        if (result.HasError)
        {
            return result.Error;
        }

        return existingScope.Value.With(scopeKey, result.Value);
    }
}