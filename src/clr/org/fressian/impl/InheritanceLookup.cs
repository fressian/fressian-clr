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
using System.Linq;

using org.fressian.handlers;

namespace org.fressian.impl
{
    public class InheritanceLookup<V> : org.fressian.handlers.ILookup<Type, V>
    {
        private readonly org.fressian.handlers.ILookup<Type, V> lookup;

        public InheritanceLookup(org.fressian.handlers.ILookup<Type, V> lookup)
        {
            this.lookup = lookup;
        }

        public V checkBaseClasses(Type c)
        {
            for (Type b = c.BaseType; b != typeof(Object); b = b.BaseType)
            {
                V val = lookup.valAt(b);
                if (val != null) return val;
            }
            return default(V);
        }

        public V checkBaseInterfaces(Type c)
        {
            IDictionary<Type, V> possibles = new Dictionary<Type, V>();
            for (Type b = c; b != typeof(Object); b = b.BaseType)
            {
                foreach (Type itf in b.GetInterfaces())
                {
                    V val = lookup.valAt(itf);
                    if (val != null) possibles[itf] = val;
                }
            }
            switch (possibles.Count)
            {
                case 0: return default(V);
                case 1: return possibles.First().Value;
                default: throw new ApplicationException("More thane one match for " + c);
            }
        }

        public V valAt(Type c)
        {
            V val = lookup.valAt(c);
            if (val == null)
            {
                val = checkBaseClasses(c);
            }
            if (val == null)
            {
                val = checkBaseInterfaces(c);
            }
            if (val == null)
            {
                val = lookup.valAt((Type)typeof(Object));
            }
            return val;
        }

        public static void main(String[] args)
        {
            IDictionary<Type, string> m = new Dictionary<Type, string>();
            m[typeof(string)] = "boo";
            org.fressian.handlers.ILookup<Type, string> ih = new InheritanceLookup<string>(new MapLookup<Type, string>(m));
            System.Console.Out.WriteLine(ih.valAt(typeof(String)));
        }
    }
}