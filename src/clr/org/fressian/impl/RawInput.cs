//   Copycchecright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.

using System;
using System.IO;
using System.Linq;

namespace org.fressian.impl
{
    public class RawInput : IDisposable
    {
        private readonly Stream stream;
        private readonly CheckedStream _checkedStream;
        private readonly BinaryReader dis;
        private int bytesRead;

        public RawInput(Stream stream) : this(stream, true)
        {
        }

        public RawInput(Stream stream, bool validateAdler)
        {
            if (validateAdler)
            {
                this._checkedStream = new CheckedStream(stream, new Adler32());
                this.stream = _checkedStream;
            }
            else
            {
                this.stream = stream;
                this._checkedStream = null;
            }
            this.dis = new BinaryReader(this.stream);
        }

        public int readRawByte()
        {
            //int result = this.stream.ReadByte();
            int result = this.dis.ReadByte();
            if (result < 0)
            {
                throw new EndOfStreamException();
            }
            bytesRead++;
            return result;
        }

        public long readRawInt8()
        {
            return readRawByte();
        }

        public long readRawInt16()
        {
            return (readRawByte() << 8) + readRawByte();
        }

        public long readRawInt24()
        {
            return (readRawByte() << 16) + (readRawByte() << 8) + readRawByte();
        }

        public long readRawInt32()
        {
            return ((readRawByte() << 24) + (readRawByte() << 16) + (readRawByte() << 8) + readRawByte()) & 0xFFFFFFFFL;
        }

        public long readRawInt40()
        {
            return (readRawInt8() << 32) | readRawInt32();
        }

        public long readRawInt48()
        {
            return (readRawInt16() << 32) | readRawInt32();
        }

        //private byte[] rawbytes = new byte[8];
        
        public long readRawInt64()
        {
            bytesRead = bytesRead + 8;
            //return dis.ReadInt64();
            
            var bytes = this.dis.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public float readRawFloat()
        {
            bytesRead = bytesRead + 4;            
            var bytes = this.dis.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double readRawDouble()
        {
            bytesRead = bytesRead + 8;
            var bytes = dis.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);            
        }

        public void readFully(byte[] bytes, int offset, int length)
        {
            this.dis.Read(bytes, offset, length);
            //this.stream.Read(bytes, offset, length);
            bytesRead += length;
        }

        public int getBytesRead()
        {
            return bytesRead;
        }

        public void Reset()
        {
            bytesRead = 0;
            if (_checkedStream != null) 
                _checkedStream.GetChecksum().Reset();
        }

        public void validateChecksum()
        {
            if (_checkedStream == null)
            {
                readRawInt32();
            }
            else
            {
                int calculatedChecksum = (int)_checkedStream.GetChecksum().Value;
                int checksumFromStream = (int)readRawInt32();
                if (calculatedChecksum != checksumFromStream)
                {
                    throw new ApplicationException(String.Format("Invalid footer checksum, expected {0} got {1}", calculatedChecksum, checksumFromStream));
                }
            }
        }

        public void Dispose()
        {
            this.dis.Close();
        }
    }
}