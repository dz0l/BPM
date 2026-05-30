namespace PrintMaestro.Services;

public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _items = [];
    private readonly LinkedList<(TKey Key, TValue Value)> _order = [];
    private readonly Lock _sync = new();

    public LruCache(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_sync)
        {
            if (!_items.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }

            _order.Remove(node);
            _order.AddFirst(node);
            value = node.Value.Value;
            return true;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_sync)
        {
            if (_items.TryGetValue(key, out var existing))
            {
                _order.Remove(existing);
                _order.AddFirst(existing);
                existing.Value = (key, value);
                return;
            }

            var node = new LinkedListNode<(TKey, TValue)>((key, value));
            _order.AddFirst(node);
            _items[key] = node;

            if (_items.Count <= _capacity)
            {
                return;
            }

            var last = _order.Last;
            if (last is null)
            {
                return;
            }

            _order.RemoveLast();
            _items.Remove(last.Value.Key);
        }
    }
}
