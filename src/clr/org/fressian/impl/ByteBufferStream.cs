//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.IO;

namespace org.fressian.impl
{
    /**
     * <code>InputStream</code> over a <code>ByteBuffer</code>. Duplicates the
     * buffer on construction in order to maintain its own cursor.
     *
     * @see     java.io.InputStream
     * @see     java.nio.ByteBuffer
     */
    public class ByteBufferStream : MemoryStream
    {
        private readonly MemoryStream _buf;

        public ByteBufferStream(MemoryStream buf)
        {
            this._buf = new MemoryStream(buf.ToArray());
        }

        public override int ReadByte()
        {
            return this._buf.ReadByte() & 0xff;
        }

        public override int Read(byte[] bytes, int off, int len)
        {
            if (len == 0) return 0;
            return this._buf.Read(bytes, off, len);
        }

        public override long Seek(long l, SeekOrigin o)
        {
            return this._buf.Seek(l, o); 
        }
    }
}