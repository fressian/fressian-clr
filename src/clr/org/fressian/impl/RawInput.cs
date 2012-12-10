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
using System.IO;
using System.Linq;

namespace org.fressian.impl
{
    public class RawInput : IDisposable
    {
        private readonly Stream stream;
        private readonly CheckedInputStream _checkedStream;
        //private readonly BinaryReader dis;
        private int bytesRead;

        public RawInput(Stream stream) : this(stream, true)
        {
        }

        public RawInput(Stream stream, bool validateAdler)
        {
            if (validateAdler)
            {
                this._checkedStream = new CheckedInputStream(stream, new Adler32());
                this.stream = _checkedStream;
            }
            else
            {
                this.stream = stream;
                this._checkedStream = null;
            }
            //this.dis = new BinaryReader(this.stream);
        }

        private byte[] internalReadBytes(byte[] bytes, int offset, int length, bool convertToBigEndian)
        {
            int readCnt = offset;
            while (readCnt < length)
            {
                var c = this.stream.Read(bytes, offset, length - readCnt);
                if (c == 0)
                    throw new EndOfStreamException();
                readCnt += c;
            }
            if(convertToBigEndian)
                Array.Reverse(bytes);
            bytesRead += length;
            return bytes;
        }

        private byte[] onebyte = new byte[1];
        public int readRawByte()
        {
            //int result = this.stream.ReadByte();
            //int result = this.dis.ReadByte();
            //if (result < 0)
            //{
            //    throw new EndOfStreamException();
            //}
            //bytesRead++;
            //return result;

            /*
            int readCnt = 0;
            while (readCnt != 1)
            {
                var c = this.stream.Read(onebyte, 0, 1);
                if (c == 0)
                    throw new EndOfStreamException();
                readCnt += c;
            }
            bytesRead++;
            */

            internalReadBytes(onebyte, 0, 1, false);
            return (int)onebyte[0];
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

        private byte[] eightbytes = new byte[8];
        public long readRawInt64()
        {
            //return dis.ReadInt64();
            
            //var bytes = this.dis.ReadBytes(8);
            //Array.Reverse(bytes);
            //return BitConverter.ToInt64(bytes, 0); 

            /*
            int readCnt = 0;
            while (readCnt != 8)
            {
                var c = this.stream.Read(eightbytes, readCnt, 8 - readCnt);
                if (c == 0)
                    throw new EndOfStreamException();
                readCnt += c;
            }
            bytesRead = bytesRead + 8;            
            Array.Reverse(eightbytes);
            */

            internalReadBytes(eightbytes, 0, 8, true);            
            return BitConverter.ToInt64(eightbytes, 0);
        }

        private byte[] fourbytes = new byte[4];
        public float readRawFloat()
        {            
            //var bytes = this.dis.ReadBytes(4);
            //Array.Reverse(bytes);
            //return BitConverter.ToSingle(bytes, 0); 

            /*
            int readCnt = 0;
            while (readCnt != 4)
            {
                var c = this.stream.Read(eightbytes, readCnt, 4 - readCnt);
                if (c == 0)
                    throw new EndOfStreamException();
                readCnt += c;
            }
            Array.Reverse(fourbytes);
            bytesRead = bytesRead + 4;            
            */

            internalReadBytes(fourbytes, 0, 4, true);            
            return BitConverter.ToSingle(fourbytes, 0);            
        }

        public double readRawDouble()
        {            
            //var bytes = dis.ReadBytes(8);
            //Array.Reverse(bytes);
            //return BitConverter.ToDouble(bytes, 0);

            /*
            int readCnt = 0;
            while (readCnt != 8)
            {
                var c = this.stream.Read(eightbytes, readCnt, 8 - readCnt);
                if (c == 0)
                    throw new EndOfStreamException();
                readCnt += c;
            }
            Array.Reverse(eightbytes);
            bytesRead = bytesRead + 8;
            */

            internalReadBytes(eightbytes, 0, 8, true);            
            return BitConverter.ToDouble(eightbytes, 0);
        }

        public void readFully(byte[] bytes, int offset, int length)
        {
            //this.dis.Read(bytes, offset, length);
            //this.stream.Read(bytes, offset, length);
            
            /*
            int readCnt = 0;
            while (readCnt < length)
            {
                var c = this.stream.Read(bytes, readCnt, length - readCnt);
                if (c == 0)
                    throw new EndOfStreamException();
                readCnt += c;
            }
            bytesRead += length;
            */

            internalReadBytes(bytes, 0, length, false);
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
                    throw new ApplicationException(String.Format("Invalid footer checksum, expected {0} got {1}", calculatedChecksum, checksumFromStream));
            }
        }

        public void Dispose()
        {
            //this.dis.Close();
        }
    }
}