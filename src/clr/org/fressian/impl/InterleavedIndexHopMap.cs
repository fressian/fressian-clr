//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.
//
//   Contributors:  Frank Failla
//

using System;
using System.Collections.Generic;

namespace org.fressian.impl
{
    public sealed class InterleavedIndexHopMap
    {
        private int cap;
        private int[] hopidx;  //[hash, idx of key, collision hash, collision idx, ...]
        private Object[] keys;
        private int count;

        public InterleavedIndexHopMap()
            : this(1024)
        {
        }

        public InterleavedIndexHopMap(int capacity)
        {
            //round up to power of 2
            int cap = 1;
            while (cap < capacity)
                cap <<= 1;

            this.cap = cap;
            hopidx = new int[this.cap << 2];
            keys = new Object[this.cap];
            count = 0;
        }

        public void clear()
        {
            count = 0;
            for (int n = 0; n < this.cap; n++)
            {
                keys[n] = null;
            }
            int cap2 = cap << 2;
            for (int n = 0; n < cap2; n++)
            {
                hopidx[n] = 0;
            }
        }

        /**
         *
         * @param k, non-null
         * @return the integer associated with k, or -1 if not present
         */
        public int get(Object k)
        {
            int hash = hashit(k);
            int mask = cap - 1;
            int bkt = (hash & mask);
            int bhash = hopidx[bkt << 2];

            if (bhash != 0)
            {
                Object bkey = keys[hopidx[(bkt << 2) + 1]];
                if (hash == bhash && k.Equals(bkey))
                    return hopidx[(bkt << 2) + 1];

                for (; (bhash = hopidx[(bkt << 2) + 2]) != 0; bkt = (bkt + 1) & mask)
                {
                    bkey = keys[hopidx[(bkt << 2) + 3]];
                    if (hash == bhash && k.Equals(bkey))
                        return hopidx[(bkt << 2) + 3];
                }
            }
            return -1;
        }

        /**
         * Puts k in the map if it was not already present.
         * Returns -1 if k was freshly added
         * Returns k's index if k was already in the map.
         * @param k, non-null
         * @return the integer associated with k or -1
         */
        public int oldIndex(Object k)
        {
            int countBefore = count;
            int index = intern(k);
            if (countBefore == count)
                return index; // already present
            else
                return -1;
        }

        public bool isEmpty()
        {
            return count == 0;
        }

        /**
         * Puts k in the map (if not present) and assigns and returns the index associated with it
         * assigns ints monotonically from 0
         * @param k, non-null
         * @return the integer associated with k
         */
        public int intern(Object k)
        {
            int hash = hashit(k);
            int mask = cap - 1;
            int bkt = (hash & mask);
            int bhash = hopidx[bkt << 2];

            int slot;
            Object bkey;

            if (bhash == 0)
                slot = bkt << 2;
            else
            {
                if (hash == bhash)
                {
                    bkey = keys[hopidx[(bkt << 2) + 1]];
                    if (k.Equals(bkey))
                        return hopidx[(bkt << 2) + 1];
                }
                for (; (bhash = hopidx[(bkt << 2) + 2]) != 0; bkt = (bkt + 1) & mask)
                {
                    if (hash == bhash)
                    {
                        bkey = keys[hopidx[(bkt << 2) + 3]];
                        if (k.Equals(bkey))
                            return hopidx[(bkt << 2) + 3];
                    }
                }
                slot = (bkt << 2) + 2;
            }

            int i = count;
            hopidx[slot] = hash;
            hopidx[slot + 1] = i;
            keys[i] = k;
            ++count;

            if (count == cap)
                resize();

            return i;
        }

        private void resize()
        {
            int[] oldhops = hopidx;
            hopidx = new int[hopidx.Length * 2];
            cap = cap << 1;
            Object[] oldkeys = keys;
            keys = new Object[cap];

            //at this point we're twice the size but empty
            Array.Copy(oldkeys, 0, keys, 0, oldkeys.Length);
            for (int slot = 0; slot < oldhops.Length; slot += 2)
            {
                int newslot = findSlot(oldhops[slot]);
                hopidx[newslot] = oldhops[slot];
                hopidx[newslot + 1] = oldhops[slot + 1];
            }
        }

        private int findSlot(int hash)
        {
            int mask = cap - 1;
            int bkt = (hash & mask);
            int bhash = hopidx[bkt << 2];
            int slot;

            if (bhash == 0)
                slot = bkt << 2;
            else
            {
                for (; hopidx[(bkt << 2) + 2] != 0; bkt = (bkt + 1) & mask)
                    ;
                slot = (bkt << 2) + 2;
            }
            return slot;
        }

        private static int hashit(Object key)
        {
            int h = key.GetHashCode();
            //we reserve 0 for no-entry
            if (h == 0)
                h = 42;
            return h;
        }

        /*
        static void report(String label, long ns)
        {
            double msdiv = 1000 * 1000 * 1000;
            System.Console.Out.WriteLine(String.Format(label + ": %2.3f", ns / msdiv));
        }

        public static void main(String[] args)
        {
            //java -server -Xmx1024m org.fressian.impl.InterleavedIndexHopMap 1000000

            int n = Convert.ToInt32(args[0]);

            IList<String> stuff = new List<String>(n);
            for (int i = 0; i < n; i++)
                stuff.Add(i.ToString());

            long start;

            start = DateTime.Now.Ticks;
            InterleavedIndexHopMap hop = new InterleavedIndexHopMap(1024);

            for (int i = 0; i < n; i++)
                hop.intern(stuff[i]);
            long hopadd = DateTime.Now.Ticks - start;

            start = DateTime.Now.Ticks;
            System.Collections.IDictionary ht = new System.Collections.Hashtable(1024);

            for (int i = 0; i < n; i++)
                ht[stuff[i]] = i;
            long htadd = DateTime.Now.Ticks - start;

            start = DateTime.Now.Ticks;
            for (int i = 0; i < n; i++)
                ht[stuff[i]] = i;
            long htadd2 = DateTime.Now.Ticks - start;

            start = DateTime.Now.Ticks;
            for (int i = 0; i < n; i++)
                hop.intern(stuff[i]);
            long hopadd2 = DateTime.Now.Ticks - start;

            start = DateTime.Now.Ticks;
            for (int i = 0; i < n; i++)
            {
                Object s = stuff[i];
                if (i != Convert.ToInt32(ht[s]))
                    System.Console.Error.WriteLine("ht can't find: " + s);
            }

            long htlookup = DateTime.Now.Ticks - start;

            start = DateTime.Now.Ticks;
            for (int i = 0; i < n; i++)
            {
                Object s = stuff[i];
                if (i != hop.get(s))
                    System.Console.Error.WriteLine("hop can't find: " + s);
            }

            if (hop.get("foobar") != -1)
                System.Console.Error.WriteLine("bad lookup succeeds!");

            long hoplookup = DateTime.Now.Ticks - start;

            report("htadd", htadd);
            report("hopadd", hopadd);
            report("htadd2", htadd2);
            report("hopadd2", hopadd2);
            report("htlookup", htlookup);
            report("hoplookup", hoplookup);
        }
        //*/
    }

}