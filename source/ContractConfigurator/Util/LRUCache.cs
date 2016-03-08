using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;

namespace ContractConfigurator.Util
{
    public class LRUCache<K, V>
    {
        class LRUCacheItem
        {
            public LRUCacheItem(K k, V v)
            {
                key = k;
                value = v;
            }
            public K key;
            public V value;
        }

        private int capacity;
        private Dictionary<K, LinkedListNode<LRUCacheItem>> cacheMap = new Dictionary<K, LinkedListNode<LRUCacheItem>>();
        private LinkedList<LRUCacheItem> lruList = new LinkedList<LRUCacheItem>();

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        public bool ContainsKey(K key)
        {
            return cacheMap.ContainsKey(key);
        }

        public void Clear()
        {
            cacheMap.Clear();
            lruList.Clear();
        }

        public V this[K key]
        {
            get
            {
                LinkedListNode<LRUCacheItem> node;
                if (cacheMap.TryGetValue(key, out node))
                {
                    V value = node.Value.value;
                    lruList.Remove(node);
                    lruList.AddLast(node);
                    return value;
                }
                return default(V);
            }
            set
            {
                Add(key, value);
            }
        }

        public void Add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                RemoveFirst();
            }

            LRUCacheItem cacheItem = new LRUCacheItem(key, val);
            LinkedListNode<LRUCacheItem> node = new LinkedListNode<LRUCacheItem>(cacheItem);
            lruList.AddLast(node);
            cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LRUCacheItem> node = lruList.First;
            lruList.RemoveFirst();

            // Remove from cache
            cacheMap.Remove(node.Value.key);
        }

        public void Save(ConfigNode node)
        {
            foreach (LRUCacheItem item in lruList)
            {
                node.AddValue(item.key.ToString(), item.value);
            }
        }

        public void Load(ConfigNode node)
        {
            foreach (ConfigNode.Value pair in node.values)
            {
                K key = (K)Convert.ChangeType(pair.name, typeof(K));
                V val = (V)Convert.ChangeType(pair.value, typeof(V));

                Add(key, val);
            }
        }
    }
}
