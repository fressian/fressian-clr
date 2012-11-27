//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;

namespace org.fressian
{
    public interface Writer
    {
        Writer writeNull();
        Writer writeBoolean(bool b);
        Writer writeBoolean(Object o);
        Writer writeInt(long l);
        Writer writeInt(Object o);
        Writer writeDouble(double d);
        Writer writeDouble(Object o);
        Writer writeFloat(float d);
        Writer writeFloat(Object o);
        Writer writeString(Object o);
        Writer writeList(Object l);
        Writer writeBytes(byte[] b);
        Writer writeBytes(byte[] b, int offset, int length);
        Writer writeObject(Object o);
        Writer writeObject(Object o, bool cache);
        Writer writeAs(String tag, Object o);
        Writer writeAs(String tag, Object o, bool cache);
        Writer writeTag(Object tag, int componentCount);
        Writer resetCaches();
        Writer writeFooter();
    }
}