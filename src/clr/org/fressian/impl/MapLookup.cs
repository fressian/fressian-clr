//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.Collections.Generic;

using org.fressian.handlers;

namespace org.fressian.impl
{
    public class MapLookup<K, V> : ILookup<K, V>
    {
        public readonly IDictionary<K, V> map;

        public MapLookup(IDictionary<K, V> map)
        {
            this.map = map;
        }

        public V valAt(K key)
        {
            if(map.ContainsKey(key))
                return map[key];
            return default(V);
        }

        //public void printMap()
        //{
        //    foreach (K k in this.map.Keys)
        //        Console.WriteLine("k={0}, v={1}", k, this.map[k]);
        //}
    }
}