using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SoPets.GOAP;

public class SPGoapContext<T, W> : IEnumerable<KeyValuePair<T, W>>, IEquatable<SPGoapContext<T, W>> where T : notnull
{
    // private static readonly Stack<SPGoapContext<T, W>> Pool = new();
    protected readonly Dictionary<T, W> Contexts;

    public SPGoapContext()
    {
        Contexts = [];
    }

    public SPGoapContext(Dictionary<T, W> contexts)
    {
        Contexts = new Dictionary<T, W>(contexts);
    }

    public SPGoapContext(T key, W value)
    {
        Contexts = new Dictionary<T, W>
        {
            { key, value }
        };
    }

    // public static SPGoapContext<T, W> Create()
    // {
    //     if (Pool.Count > 0)
    //     {
    //         var context = Pool.Pop();
    //         context.Reset();
    //         return context;
    //     }

    //     return new SPGoapContext<T, W>();
    // }

    // public static SPGoapContext<T, W> Create(Dictionary<T, W> contexts)
    // {
    //     var context = Create();
    //     foreach (var kvp in contexts)
    //     {
    //         context.Contexts[kvp.Key] = kvp.Value;
    //     }
    //     return context;
    // }

    // public SPGoapContext<T, W> Clone()
    // {
    //     var clone = Create();

    //     foreach (var kvp in Contexts)
    //     {
    //         clone.Contexts[kvp.Key] = kvp.Value;
    //     }

    //     return clone;
    // }

    public SPGoapContext<T, W> Clone()
    {
        var clone = new SPGoapContext<T, W>();

        foreach (var kvp in Contexts)
        {
            clone.Contexts[kvp.Key] = kvp.Value;
        }

        return clone;
    }

    // public void Release()
    // {
    //     Reset();

    //     if (Pool.Count < MAX_POOL_SIZE)
    //     {
    //         Pool.Push(this);
    //         return;
    //     }
    // }

    public void Reset()
    {
        Contexts.Clear();
    }

    public Dictionary<T, W> GetContext()
    {
        return Contexts;
    }

    public bool TryGetContext(T key, [MaybeNullWhen(false)] out W value)
    {
        if (Contexts.TryGetValue(key, out value))
        {
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }

    public void SetContext(T key, W value)
    {
        Contexts[key] = value;
    }

    public bool HasContext(T key)
    {
        return Contexts.ContainsKey(key);
    }

    public bool HasAny(params T[] keys)
    {
        return keys.Any(Contexts.ContainsKey);
    }

    public bool RemoveContext(T key)
    {
        return Contexts.Remove(key);
    }

    public void ClearContexts()
    {
        Contexts.Clear();
    }

    public IEnumerator<KeyValuePair<T, W>> GetEnumerator()
    {
        return Contexts.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Equals(SPGoapContext<T, W>? other)
    {
        return
        other != null &&
        Contexts.Count == other.Contexts.Count &&
        Contexts.All(kvp => other.Contexts.TryGetValue(kvp.Key, out var value) && EqualityComparer<W>.Default.Equals(kvp.Value, value));
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        return Equals(obj as SPGoapContext<T, W>);
    }

    public static bool operator ==(SPGoapContext<T, W>? left, SPGoapContext<T, W>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(SPGoapContext<T, W>? left, SPGoapContext<T, W>? right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        if (Contexts == null || Contexts.Count == 0)
            return 0;

        int hash = 0;
        foreach (var kvp in Contexts)
        {
            int itemHash = HashCode.Combine(kvp.Key, kvp.Value);
            hash ^= itemHash;
        }

        return hash;
    }

    public override string ToString()
    {
        if (Contexts == null || Contexts.Count == 0)
        {
            return "{}";
        }

        return "{" + string.Join(", ", Contexts.Select(kvp => $"{kvp.Key}: {kvp.Value}")) + "}";
    }
}