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
    public class ByteBufferInputStream : MemoryStream
    {
        private readonly MemoryStream buf;

        public ByteBufferInputStream(MemoryStream buf)
        {
            buf.CopyTo(this.buf, buf.Capacity);
        }

        public override int ReadByte()
        {
            return this.buf.ReadByte();
            
            //if (!buf.r .hasRemaining()) {
            //    return -1;
            //}
            //int result = buf.get();
            //return result & 0xff;
        }

        public override int Read(byte[] bytes, int off, int len)
        {
            return this.buf.Read(bytes, off, len);

            //if (len == 0) return 0;
            //int bytesRead = Math.Min((int) len, buf.remaining());
            //if (bytesRead <= 0) {
            //    return -1;
            //}
            //buf.get(bytes, off, bytesRead);
            //return bytesRead;
        }

        public override long Seek(long l, SeekOrigin o)
        {
            return this.buf.Seek(l, o);
           
            // note: buf.remaining() can be negative
            //int skipped = Math.Min((int)l, (int)(buf.Length - buf.Position));
            //if (skipped <= 0)
            //{
            //    return 0;
            //}
            //buf.Position = buf.Position + skipped;
            //return skipped;
        }

        public int available()
        {
            return (int)(this.buf.Length - this.buf.Position);
        }
    }
}