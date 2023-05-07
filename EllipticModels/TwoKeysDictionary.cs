using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EllipticModels
{
    public class TwoKeysDictionary<K1, K2, V> : Dictionary<K1, Dictionary<K2, V>>
    {
        public V this[K1 key1, K2 key2]
        {
            get
            {
                if (!ContainsKey(key1) || !this[key1].ContainsKey(key2))
                    throw new ArgumentOutOfRangeException();
                return base[key1][key2];
            }
            set
            {
                Add(key1, key2, value);
            }
        }


        public bool TryGetValue(K1 key1, K2 key2, out V value)
        {
            value = default(V);
            if (!ContainsKey(key1) || !this[key1].ContainsKey(key2))
            {
                return false;
            }
            else
            {
                value = base[key1][key2];
                return true;
            }
        }


        public void Add(K1 key1, K2 key2, V value)
        {
            if (!ContainsKey(key1))
                this[key1] = new Dictionary<K2, V>();
            this[key1][key2] = value;
        }

        public bool ContainsKey(K1 key1, K2 key2)
        {
            return base.ContainsKey(key1) && this[key1].ContainsKey(key2);
        }

        public new IEnumerable<V> Values
        {
            get
            {
                return from baseDict in base.Values
                       from baseKey in baseDict.Keys
                       select baseDict[baseKey];
            }
        }

    }



    public class ConcurrentTwoKeysDictionary<K1, K2, V> : ConcurrentDictionary<K1, ConcurrentDictionary<K2, V>>
    {
        public V this[K1 key1, K2 key2]
        {
            get
            {
                if (!ContainsKey(key1) || !this[key1].ContainsKey(key2))
                    throw new ArgumentOutOfRangeException();
                return base[key1][key2];
            }
            set
            {
                Add(key1, key2, value);
            }
        }


        public bool TryGetValue(K1 key1, K2 key2, out V value)
        {
            value = default(V);
            if (!ContainsKey(key1) || !this[key1].ContainsKey(key2))
            {
                return false;
            }
            else
            {
                value = base[key1][key2];
                return true;
            }
        }


        public bool Add(K1 key1, K2 key2, V value)
        {
            if (!ContainsKey(key1))
            {
                this[key1] = new ConcurrentDictionary<K2, V>();
            }

            V new_value = this[key1].AddOrUpdate(key2, value, (k, v) =>
            {
                return value;
            });
            return new_value != null;
        }

        public bool ContainsKey(K1 key1, K2 key2)
        {
            return base.ContainsKey(key1) && this[key1].ContainsKey(key2);
        }

        public new IEnumerable<V> Values
        {
            get
            {
                return from baseDict in base.Values
                       from baseKey in baseDict.Keys
                       select baseDict[baseKey];
            }
        }

    }

    public class ConcurrentMultikeysDictionary<K1, K2, V> : ConcurrentDictionary<(K1, K2), V>
    {
        public V this[K1 key1, K2 key2]
        {
            get
            {
                if (!ContainsKey(key1, key2))
                    return default(V);
                return base[(key1, key2)];
            }
            set
            {
                base.TryAdd((key1, key2), value);
            }
        }
        public bool TryAdd(K1 key1, K2 key2, V value)
        {
            return base.TryAdd((key1, key2), value);
        }

        public bool TryAddOrUpdate(K1 key1, K2 key2, V value)
        {
            if (TryGetValue((key1, key2), out V old_value))
            {
                return base.TryUpdate((key1, key2), value, old_value);
            }
            else
            {
                return base.TryAdd((key1, key2), value);
            }
        }


        public bool TryGetValue(K1 key1, K2 key2, out V value)
        {
            value = default(V);
            if (ContainsKey(key1, key2))
            {
                return base.TryGetValue((key1, key2), out value);
            }
            return false;
        }

        public bool ContainsKey(K1 key1, K2 key2)
        {
            return base.ContainsKey((key1, key2));
        }
    }
}
