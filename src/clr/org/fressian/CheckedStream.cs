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
            var c = this._stream.Read(buffer, offset, count);
            if(c > 0) //if not at end-of-stream
                this._checksum.Update(buffer, offset, count);
            return c;
        }

        public override int ReadByte()
        {
            var b = this._stream.ReadByte();
            if(b != -1)
                this._checksum.Update(new byte[] { (byte)b }, 0, 1);
            return b;            
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this._checksum.Update(buffer, offset, count);
            this._stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            this._checksum.Update(new byte[] {value}, 0, 1);
            this._stream.WriteByte(value);
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