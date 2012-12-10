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
using System.Collections;

namespace org.fressian
{
    public class TaggedObject : Tagged
    {
        public readonly Object tag;
        public readonly Object[] value;
        public readonly IDictionary meta;

        public TaggedObject(Object tag, Object[] value)
            : this(tag, value, null)
        {
        }

        public TaggedObject(Object tag, Object[] value, IDictionary meta)
        {
            this.meta = meta;
            this.value = value;
            this.tag = tag;
        }

        public Object Tag
        {
            get { return tag; }
        }

        public Object[] Value
        {
            get { return value;}
        }

        public IDictionary Meta
        {
            get { return meta; }
        }
    }
}