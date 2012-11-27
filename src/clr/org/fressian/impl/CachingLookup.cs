//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.Collections.Concurrent;
using System.Threading;

using org.fressian.handlers;

namespace org.fressian.impl
{
    // could refine to keep track of lookup misses...
    public class CachingLookup<K, V> : ILookup<K, V> where V : class
    {
        public readonly ILookup<K, V> lookup;
        public readonly ConcurrentDictionary<K, V> map = new ConcurrentDictionary<K, V>();
        //FF no equiv public readonly AtomicReference<V> nullKeyValue = new AtomicReference(null);
        public V nullKeyValue = null;

        public CachingLookup(ILookup<K, V> lookup)
        {
            this.lookup = lookup;
        }

        private V getNullVal()
        {
            V val = nullKeyValue;
            if (val != null) return val;
            val = nullKeyValue;
            if (val != null) Interlocked.Exchange<V>(ref nullKeyValue, val);
            return val;
        }

        public V valAt(K key)
        {
            if (key == null) return getNullVal();
            if (map.ContainsKey(key))
                return map[key];
            V val = lookup.valAt(key);
            if (val != null) map.GetOrAdd(key, val);
            return val;
        }
    }
}