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
            set { throw new InvalidOperationException(); }
        }

        public override long Length
        {
            get { return this._stream.Length; }
        }

        public CheckedStream(Stream ostream, Checksum checksum)
        {
            this._stream = ostream;
            this._checksum = checksum;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this._checksum.Update(buffer, offset, count);
            return this._stream.Read(buffer, offset, count);
        }
       
        public override void Write(byte[] buffer, int offset, int count)
        {
            this._checksum.Update(buffer, offset, count);
            this._stream.Write(buffer, offset, count);
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