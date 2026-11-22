using System.Collections;
using System.Collections.Immutable;

namespace Pathetic.Slang.Generator;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _items;

    public EquatableArray(ImmutableArray<T> items)
    {
        _items = items;
    }

    public int Length => _items.IsDefault ? 0 : _items.Length;

    public T this[int index] => _items[index];

    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

    public static EquatableArray<T> From(IEnumerable<T> values) => new([.. values]);

    public bool Equals(EquatableArray<T> other)
    {
        if (Length != other.Length)
            return false;

        for (int i = 0; i < Length; i++)
        {
            if (!_items[i].Equals(other._items[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            foreach (T item in this)
                hash = (hash * 31) + (item is null ? 0 : item.GetHashCode());

            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (T item in _items)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
