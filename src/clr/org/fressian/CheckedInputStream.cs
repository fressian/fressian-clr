//   Copyright (c) Metadata Partners, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.
//
//   Author:  Frank Failla
//

using System;
using System.IO;

namespace org.fressian
{
    public class CheckedInputStream : System.IO.Stream
    {
        protected Stream _stream;
        protected Checksum _checksum;

        public override bool CanRead 
        { 
            get { return true; } 
        }
        
        public override bool CanSeek 
        { 
            get { return false; } 
        }
        
        public override bool CanWrite 
        { 
            get { return false; } 
        }

        public override long Position
        {
            get { return this._stream.Position; }
            set { throw new InvalidOperationException("Setting the position on a CheckedIntputStream is not permitted."); }
        }

        public override long Length
        {
            get { return this._stream.Length; }
        }

        public CheckedInputStream(Stream stream, Checksum checksum)
        {
            this._stream = stream;
            this._checksum = checksum;
        }
       
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesread = 0;
            while (bytesread != count)
            {
                var c = this._stream.Read(buffer, offset+bytesread, count-bytesread);
                if (c == 0) //end-of-stream
                    break;
                bytesread += c;
            }
            if (bytesread > 0) //if any bytes were read
                this._checksum.Update(buffer, offset, count);
            return bytesread;
        }
        
        public override int ReadByte()
        {
            var b = this._stream.ReadByte();
            if (b != -1)  //if not at end-of-stream
                this._checksum.Update(new byte[] { (byte)b }, 0, 1);
            return b;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Only read operations are supported on a CheckedInputStream");
        }

        public override void WriteByte(byte value)
        {
            throw new InvalidOperationException("Only read operations are supported on a CheckedInputStream");
        }

        public override void Flush()
        {
            this._stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Seek is not supported on a CheckedInputStream");
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("SetLength is not supported on a CheckedInputStream");
        }

        public Checksum GetChecksum()
        {
            return this._checksum;
        }        
    }
}