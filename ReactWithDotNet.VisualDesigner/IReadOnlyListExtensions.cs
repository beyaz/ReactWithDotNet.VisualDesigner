global using static ReactWithDotNet.VisualDesigner.IReadOnlyListExtensions;
using System.Collections.Immutable;

namespace ReactWithDotNet.VisualDesigner;

static class IReadOnlyListExtensions
{
    public static IReadOnlyList<T> Add<T>(this IReadOnlyList<T> readOnlyList, T value)
    {
        return readOnlyList.ToImmutableList().Add(value);
    }
}