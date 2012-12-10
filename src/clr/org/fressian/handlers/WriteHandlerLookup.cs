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

using org.fressian.impl;

namespace org.fressian.handlers
{
    public class WriteHandlerLookup : IWriteHandlerLookup
    {
        private readonly ILookup<Type, IDictionary<String, WriteHandler>> chainedLookup;

        public static ILookup<Type, IDictionary<String, WriteHandler>> createLookupChain(ILookup<Type, IDictionary<String, WriteHandler>> userHandlers)
        {
            if (userHandlers != null)
            {
                return new CachingLookup<Type, IDictionary<String, WriteHandler>>
                    (new ChainedLookup<Type, IDictionary<String, WriteHandler>>(Handlers.coreWriteHandlers, userHandlers, Handlers.extendedWriteHandlers));
            }
            else
            {
                return Handlers.defaultWriteHandlers();
            }
        }

        public WriteHandler getWriteHandler(String tag, Object o)
        {
            IDictionary<String, WriteHandler> h = Fns.lookup<Type, IDictionary<string, WriteHandler>>(chainedLookup, Fns.getClassOrNull(o));
            if (h == null)
                return null;
            KeyValuePair<String, WriteHandler> taggedWriter = Fns.soloEntry(h);
            if (tag != null && !tag.Equals(taggedWriter.Key) && !taggedWriter.Key.Equals("any"))
            {
                return null;
            }
            else
            {
                return taggedWriter.Value;
            }
        }

        public WriteHandler requireWriteHandler(String tag, Object o)
        {
            WriteHandler handler = getWriteHandler(tag, o);
            if (handler == null)
                throw new ArgumentOutOfRangeException("Cannot write " + o + " as tag " + tag);
            return handler;
        }

        public WriteHandlerLookup(ILookup<Type, IDictionary<String, WriteHandler>> userHandlers)
        {
            this.chainedLookup = WriteHandlerLookup.createLookupChain(userHandlers);
        }
    }
}