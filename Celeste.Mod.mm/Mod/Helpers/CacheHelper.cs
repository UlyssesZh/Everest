using System.Collections.Generic;

namespace Celeste.Mod.Helpers {
    /**
     * A helper class that wraps a <code>compute</code> function, in order to keep the
     * latest computed values in memory (holding up to <code>maxSize</code> values),
     * to help with performance or with repeated memory allocations.
     */
    public abstract class CacheHelper<Key, Value> {
        private readonly string name;
        private readonly int maxSize;
        private readonly Dictionary<Key, Value> cache = new Dictionary<Key, Value>();
        private readonly LinkedList<Key> cacheOldestToNewest = new LinkedList<Key>();

        public CacheHelper(string name, int maxSize) {
            this.name = name;
            this.maxSize = maxSize;
        }

        public Value GetCached(Key key) {
            lock (cache) {
                if (cache.TryGetValue(key, out Value value)) {
                    LinkedListNode<Key> node = cacheOldestToNewest.Find(key);
                    cacheOldestToNewest.Remove(node);
                    cacheOldestToNewest.AddLast(node);
                    return value;
                }

                value = compute(key);
                while (cache.Count >= maxSize) {
                    Key keyToEvict = cacheOldestToNewest.First.Value;
                    cache.Remove(keyToEvict);
                    cacheOldestToNewest.RemoveFirst();
                }

                cache.Add(key, value);
                cacheOldestToNewest.AddLast(key);
                Logger.Verbose("CacheHelper", $"[{name}] Cached value for \"{key}\" => \"{value}\", cache size: {cache.Count}");
                return value;
            }
        }

        protected abstract Value compute(Key key);

        public void Clear() {
            lock (cache) {
                cache.Clear();
                cacheOldestToNewest.Clear();
            }
        }
    }
}
