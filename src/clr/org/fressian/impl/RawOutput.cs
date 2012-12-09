//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.IO;

using org.fressian;

namespace org.fressian.impl
{    
    public class RawOutput : IDisposable
    {
        private readonly CheckedOutputStream _checkedStream;
        private int bytesWritten;
        private BinaryWriter writer;

        public RawOutput(Stream stream)
        {
            this._checkedStream = new CheckedOutputStream(stream, new Adler32());
            this.writer = new BinaryWriter(this._checkedStream);
        }

        public void writeRawByte(int b)
        {
            this.writer.Write((byte)b);
            notifyBytesWritten(1);
        }

        public void writeRawInt16(int s)
        {
            this.writer.Write((byte)(((uint)s >> 8) & 0xFF));
            this.writer.Write((byte)(s & 0xFF));
            notifyBytesWritten(2);
        }

        public void writeRawInt24(int i)
        {
            this.writer.Write((byte)(((uint)i >> 16) & 0xFF));
            this.writer.Write((byte)(((uint)i >> 8) & 0xFF));
            this.writer.Write((byte)(i & 0xFF));
            notifyBytesWritten(3);
        }

        public void writeRawInt32(int i)
        {
            this.writer.Write((byte)(((uint)i >> 24) & 0xFF));
            this.writer.Write((byte)(((uint)i >> 16) & 0xFF));
            this.writer.Write((byte)(((uint)i >> 8) & 0xFF));
            this.writer.Write((byte)((uint)i & 0xFF));
            notifyBytesWritten(4);
        }

        public void writeRawInt40(long i)
        {
            this.writer.Write((byte)(((ulong)i >> 32) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 24) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 16) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 8) & 0xFF));
            this.writer.Write((byte)((ulong)i & 0xFF));
            notifyBytesWritten(5);
        }

        public void writeRawInt48(long i)
        {
            this.writer.Write((byte)(((ulong)i >> 40) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 32) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 24) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 16) & 0xFF));
            this.writer.Write((byte)(((ulong)i >> 8) & 0xFF));
            this.writer.Write((byte)((ulong)i & 0xFF));
            notifyBytesWritten(6);
        }

        byte[] buffer = new byte[8];
        
        public void writeRawInt64(long l)
        {
            buffer[0] = (byte)((ulong)l >> 56);
            buffer[1] = (byte)((ulong)l >> 48);
            buffer[2] = (byte)((ulong)l >> 40);
            buffer[3] = (byte)((ulong)l >> 32);
            buffer[4] = (byte)((ulong)l >> 24);
            buffer[5] = (byte)((ulong)l >> 16);
            buffer[6] = (byte)((ulong)l >> 8);
            buffer[7] = (byte)((ulong)l >> 0);
            this.writer.Write(buffer, 0, 8);
            notifyBytesWritten(8);
        }

        public void writeRawDouble(double d)
        {
            writeRawInt64(BitConverter.DoubleToInt64Bits(d));
        }

        public void writeRawFloat(float f)
        {
            writeRawInt32(Fns.SingleToInt32Bits(f));
        }

        public void writeRawBytes(byte[] bytes, int off, int len)
        {
            this.writer.Write(bytes, off, len);
            notifyBytesWritten(len);
        }

        public Checksum GetChecksum()
        {
            return this._checkedStream.GetChecksum();
        }

        public int getBytesWritten()
        {
            return bytesWritten;
        }

        public void Reset()
        {
            bytesWritten = 0;
            this.GetChecksum().Reset();
        }

        private void notifyBytesWritten(int count)
        {
            bytesWritten = bytesWritten + count;
        }
       
        public void Dispose()
        {
            this.writer.Close();
        }
    }
}