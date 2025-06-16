using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.Helpers {
    public static class UnicodeStringHelper {
        // helper class because manipulating generic types in IL is annoying
        public class ListInt {
            public required int[] Elements;
            public int Count => Elements.Length;
            public int this[int index] => Elements[index];
        }

        private const int CACHE_MAX_SIZE = 1000;
        private static readonly Dictionary<string, ListInt> cache = new Dictionary<string, ListInt>();
        private static readonly LinkedList<string> cacheOldestToNewest = new LinkedList<string>();

        public static ListInt ToCodePointList(string text) {
            lock (cache) {
                if (cache.TryGetValue(text, out ListInt list)) {
                    LinkedListNode<string> node = cacheOldestToNewest.Find(text);
                    cacheOldestToNewest.Remove(node);
                    cacheOldestToNewest.AddLast(node);
                    return list;
                }

                list = new ListInt {
                    Elements = text.EnumerateRunes().Select(r => r.Value).ToArray()
                };
                while (cache.Count >= CACHE_MAX_SIZE) {
                    string textToEvict = cacheOldestToNewest.First.Value;
                    cache.Remove(textToEvict);
                    cacheOldestToNewest.RemoveFirst();
                }

                cache.Add(text, list);
                cacheOldestToNewest.AddLast(text);
                Logger.Verbose("UnicodeStringHelper", $"Cached unicode code point list for \"{text}\", cache size: {cache.Count}");
                return list;
            }
        }
    }
}
