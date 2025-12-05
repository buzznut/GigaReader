//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace UtilitiesLibrary;

public class MRUCache<K, V> where K : notnull, IEquatable<K>, IComparable<K>
{
    private object  cacheLock = new object();
    private readonly Dictionary<K, MRUData<V>> cache = new Dictionary<K, MRUData<V>>();
    private readonly LinkedList<K> mru = new LinkedList<K>();
    private readonly int maxCount;
    private readonly int keepPurgeCount;
    private readonly TimeSpan maxAge;
    private readonly Queue<KeyValuePair<K,V>> purgedItems = new Queue<KeyValuePair<K,V>>();
    private readonly long ticks = DateTimeOffset.Now.Ticks;
    private readonly long maxAgeTicks = -1;

#if DEBUG
    private Dictionary<string, StackTrace> allocations = new Dictionary<string, StackTrace>();
#endif

    // -=-=-= public methods -=-=-=

    public MRUCache() : this(default, default)
    {
    }

    public MRUCache(int maxCount) : this(maxCount, default)
    {
    }

    public MRUCache(TimeSpan maxAge) : this(default, maxAge)
    {
    }

    public int Count => mru.Count;

    public MRUCache(int maxCount = default, TimeSpan maxAge = default, int keepPurgeCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAge, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCount);
        this.maxAge = maxAge;
        this.maxCount = maxCount;
        this.keepPurgeCount = keepPurgeCount;
        if (maxAge != default) maxAgeTicks = (new DateTimeOffset(ticks, TimeSpan.Zero) + maxAge).Ticks - ticks;
    }

    public ICollection<KeyValuePair<K,V>> GetPurged()
    {
        lock (cacheLock)
        {
            return _GetPurged;
        }
    }

    public void Clear()
    {
        lock (cacheLock)
        {
            _Clear();
        }
    }

    public int Purge()
    {
        lock (cacheLock)
        {
            return _Purge();
        }
    }

    public V this[K key]
    {
        get
        {
            lock (cacheLock)
            {
                TryResult<V> result = _TryGetValue(key);
                return result.Value;
            }
        }

        set
        {
            lock (cacheLock)
            {
                _AddOrUpdate(key, value);
            }
        }
    }

    public V AddOrUpdate(K key, V value)
    {
        lock (cacheLock)
        {
            return _AddOrUpdate(key, value);
        }
    }

    public V Remove(K key)
    {
        lock (cacheLock)
        {
            return _Remove(key);
        }
    }

    public ICollection<V> Values
    {
        get
        {
            lock (cacheLock)
            {
                return _Values;
            }
        }
    }

    public ICollection<K> Keys
    {
        get
        {
            lock (cacheLock)
            {
                return _Keys;
            }
        }
    }

    public K First
    {
        get
        {
            lock (cacheLock)
            {
                return _First;
            }
        }
    }

    public K Last
    {
        get
        {
            lock (cacheLock)
            {
                return _Last;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (cacheLock)
            {
                return _IsEmpty;
            }
        }
    }

    public bool ContainsKey(K key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        lock (cacheLock)
        {
            return _ContainsKey(key);
        }
    }

    public bool TryRemove(K key, [MaybeNullWhen(false)] out V value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        lock (cacheLock)
        {
            TryResult<V> tryResult = _TryRemove(key);
            value = tryResult.Value;
            return tryResult.Result;
        }
    }

    public bool TryGetValue(K key, out V value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        lock (cacheLock)
        {
            TryResult<V> tryResult = _TryGetValue(key);
            value = tryResult.Value;
            return tryResult.Result;
        }
    }

    // -=-=-= private methods -=-=-=

    private ICollection<KeyValuePair<K, V>> _GetPurged
    {
        get
        {
            KeyValuePair<K, V>[] items = purgedItems.ToArray();
            purgedItems.Clear();
            return items;
        }
    }

    private void _Clear()
    {
        cache.Clear();
        mru.Clear();
        purgedItems.Clear();
    }

    private int _Purge()
    {
        int purgeCount = 0;

        if (maxAge != default)
        {
            long nowTicks = DateTimeOffset.Now.Ticks;

            foreach (K key in cache.Keys)
            {
                MRUData<V> data = cache[key];
                if ((nowTicks - data.Created) >= maxAgeTicks)
                {
                    purgeCount++;
                    V value = _Remove(key);
                    if (keepPurgeCount > 0)
                    {
                        purgedItems.Enqueue(new KeyValuePair<K, V>(key, value));
                    }
                }
            }
        }

        if (maxCount != default)
        {
            while (cache.Count > maxCount)
            {
                if (mru.Last == null) break;
                LinkedListNode<K> node = mru.Last;

                K key = node.Value;
                purgeCount++;
                V value = _Remove(key);
                if (keepPurgeCount > 0)
                {
                    purgedItems.Enqueue(new KeyValuePair<K, V>(key, value));
                }
            }
        }

        return purgeCount;
    }

    private ICollection<K> _Keys
    {
        get
        {
            List<K> keys;
            keys = new List<K>(cache.Count);
            foreach (K key in mru)
            {
                keys.Add(key);
            }
            return keys;
        }
    }

    private K _First
    {
        get
        {
            LinkedListNode<K> node = mru.First;
            return node == null ? default : node.Value;
        }
    }

    private K _Last
    {
        get
        {
            LinkedListNode<K> node = mru.Last;
            return node == null ? default : node.Value;
        }
    }

    private bool _IsEmpty => cache.Count == 0;

    private bool _ContainsKey(K key)
    {
        return cache.ContainsKey(key);
    }

    private TryResult<V> _TryRemove(K key)
    {
        if (cache.Remove(key, out MRUData<V> data))
        {
            TryResult<V> result = new TryResult<V>(false, default);
            result.Value = data.Value;
            result.Result = mru.Remove(key);
            if (!result.Result) throw new ApplicationException("Could not find node");
            return result;
        }

        return new TryResult<V>(false);
    }

    private V _AddOrUpdate(K key, V value)
    {
        bool exists = cache.ContainsKey(key);
        if (exists)
        {
            // key exists
            MRUData<V> data = cache[key];
            data.Value = value;

            // must find the key in the linked list
            LinkedListNode<K> node = mru.Find(key);
            if (node == null) throw new ApplicationException("Could not find node");

            // put the existing node at the beginning of the list
            if (node.Previous != null)
            {
                mru.Remove(node);
                mru.AddFirst(node);
            }
        }
        else
        {
            // key does not exist
            cache[key] = new MRUData<V>(value, ticks);

            // set the new node at the beginning of the list
            mru.AddFirst(key);

            _Purge();
        }

        return value;
    }

    private V _Remove(K key)
    {
        cache.Remove(key, out MRUData<V> data);
        if (data == default) return default;

        // ReSharper disable once InconsistentlySynchronizedField
        bool result = mru.Remove(key);
        if (!result) throw new ApplicationException("Could not find node");

        return data.Value;
    }

    private ICollection<V> _Values
    {
        get
        {
            List<V> values;

            values = new List<V>(cache.Count);
            foreach (K key in mru)
            {
                values.Add(cache[key].Value);
            }

            return values;
        }
    }

    private TryResult<V> _TryGetValue(K key)
    {
        if (cache.TryGetValue(key, out MRUData<V> data))
        {
            // must find the key in the linked list
            LinkedListNode<K> node = mru.Find(key);
            if (node == null) throw new ApplicationException("Could not find node");
            // put the existing node at the beginning of the list
            if (node.Previous != null)
            {
                mru.Remove(node);
                mru.AddFirst(node);
            }

            return new TryResult<V>(true, data.Value);
        }

        return new TryResult<V>(false, default);
    }
}

public class MRUData<V>
{
    public long Created { get; set; }
    public V Value { get; set; } = default;

    public MRUData(V value, long ticks) : this(ticks)
    {
        Value = value;
    }

    public MRUData(long ticks)
    {
        Created = DateTimeOffset.Now.Ticks - ticks;
    }
}

public class TryResult<V> 
{
    public bool Result { get; set; }
    public V Value { get; set; }
    public TryResult(bool result, V value = default)
    {
        Result = result;
        Value = value;
    }
}
