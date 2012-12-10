//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.
//
//   Author:  Frank Failla
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace org.fressian
{
    public class ImmutableDictionary<K, V> : IDictionary<K, V>
    {
        IDictionary<K, V> _d;

        public ImmutableDictionary(IDictionary<K, V> d)
        {
            this._d = d;
        }

        public void Add(K key, V value)
        {
            throw new InvalidOperationException();
        }

        public bool ContainsKey(K key)
        {
            return this._d.ContainsKey(key);
        }

        public ICollection<K> Keys
        {
            get { return this._d.Keys; }
        }

        public bool Remove(K key)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetValue(K key, out V value)
        {
            return this._d.TryGetValue(key, out value);
        }

        public ICollection<V> Values
        {
            get { return this._d.Values; }
        }

        public V this[K key]
        {
            get { return this._d[key]; }
            set { throw new InvalidOperationException(); }
        }

        public void Add(KeyValuePair<K, V> item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return this._d.Contains(item);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            this._d.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this._d.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            throw new InvalidOperationException();
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return this._d.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)this._d).GetEnumerator();
        }
    }
}