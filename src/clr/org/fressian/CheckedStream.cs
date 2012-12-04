using System;
using System.IO;

namespace org.fressian
{
    public class CheckedStream : System.IO.Stream
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
            get { return true; } 
        }

        public override long Position
        {
            get { return this._stream.Position; }
            set { throw new InvalidOperationException("setting the position on a checked stream is not permitted."); }
        }

        public override long Length
        {
            get { return this._stream.Length; }
        }

        public CheckedStream(Stream stream, Checksum checksum)
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
            this._stream.Write(buffer, offset, count);
            this._checksum.Update(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            this._stream.WriteByte(value);
            this._checksum.Update(new byte[] { value }, 0, 1);
        }

        public override void Flush()
        {
            this._stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public Checksum GetChecksum()
        {
            return this._checksum;
        }        
    }
}